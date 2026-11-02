using Core.DataStructures;
using Core.IO;
using Core.PostProcessing;
using Core.Structures;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;
using System;
using System.Collections.Generic;
using System.Numerics;
using Visualizer;

class Program
{
    private static readonly Dictionary<int, float> _loopWeights = new();
    private static readonly Dictionary<int, double[]> _snapWeightsCache = new();

    static void Main(string[] args)
    {
        // 1. Load Mesh Data (assumes a closed, genus-0 mesh)
        IMeshImporter importer = MeshImporterFactory.Resolve("obj");
        MeshData data = importer.Load("Assets/Models/lion.obj");

        Vertex[] baseVertices = data.Vertices;
        int[] baseIndices = data.Indices;

        NormalsGenerator.Apply(baseVertices, baseIndices, NormalsMode.Smooth);

        // 2. Build Topology for the base mesh
        CornerTable baseTable = new CornerTable(baseIndices, baseVertices.Length);

        // 3. Compute Loop Subdivision and Limit Surface Snapping
        (Vertex[] loopVertices, int[] loopIndices) = Loop(baseVertices, baseIndices, baseTable);
        Vertex[] snappedVertices = SnapLoop(baseVertices, baseIndices, baseTable);

        // Frame 0: Original Mesh
        // Frame 1: 1x Loop Subdivision
        // Frame 2: Original Mesh Snapped to Loop Limit Positions
        Vertex[][] frames =
        [
            Vertex.Clone(baseVertices),
            loopVertices,
            snappedVertices
        ];

        int[][] frameIndices =
        [
            baseIndices,
            loopIndices,
            baseIndices
        ];

        // 4. Launch Visualizer (use Left / Right arrows to compare meshes)
        VisualizerSession.Run(session =>
        {
            session.SetAnimation(frames, frameIndices);
        });
    }

    #region Task 1: Connectivity Subdivision (2 points)

    public static (Vertex[] Vertices, int[] Indices) Loop(Vertex[] vertices, int[] indices, CornerTable table)
    {
        int faceCount = indices.Length / 3;

        // Closed, genus-0 mesh assumption: V_new = V + 3F / 2
        Vertex[] newVertices = new Vertex[vertices.Length + (faceCount * 3) / 2];

        // 1. Update old vertices
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 pos = OldPoint(vertices, table, i);
            newVertices[i] = Vertex.Create(pos.X, pos.Y, pos.Z);
        }

        // 2. Generate new edge vertices (one per edge, opposite to corner i)
        int index = vertices.Length;
        int[] edgeIndices = new int[table.opposite.Length];
        for (int i = 0; i < table.opposite.Length; i++)
        {
            // Each edge is shared by two opposite corners; pick the one with the smaller index
            if (i < table.opposite[i])
            {
                Vector3 pos = NewPoint(vertices, table, i);
                newVertices[index] = Vertex.Create(pos.X, pos.Y, pos.Z);
                edgeIndices[i] = index;
                edgeIndices[table.opposite[i]] = index;
                index++;
            }
        }

        // 3. Construct 4 new triangles per old triangle (dyadic split)
        int[] newIndices = new int[indices.Length * 4];
        for (int i = 0; i < faceCount; i++)
        {
            int c1 = 3 * i;
            int c2 = 3 * i + 1;
            int c3 = 3 * i + 2;

            int v1 = indices[c1];
            int v2 = indices[c2];
            int v3 = indices[c3];

            int e1 = edgeIndices[c1];
            int e2 = edgeIndices[c2];
            int e3 = edgeIndices[c3];

            int baseIdx = 12 * i;

            // Corner triangle at v1: (v1, e3, e2)
            newIndices[baseIdx] = v1;
            newIndices[baseIdx + 1] = e3;
            newIndices[baseIdx + 2] = e2;

            // Corner triangle at v3: (v3, e2, e1)
            newIndices[baseIdx + 3] = v3;
            newIndices[baseIdx + 4] = e2;
            newIndices[baseIdx + 5] = e1;

            // Corner triangle at v2: (v2, e1, e3)
            newIndices[baseIdx + 6] = v2;
            newIndices[baseIdx + 7] = e1;
            newIndices[baseIdx + 8] = e3;

            // Center triangle: (e1, e2, e3)
            newIndices[baseIdx + 9] = e1;
            newIndices[baseIdx + 10] = e2;
            newIndices[baseIdx + 11] = e3;
        }

        NormalsGenerator.Apply(newVertices, newIndices, NormalsMode.Smooth);
        return (newVertices, newIndices);
    }

    #endregion

    #region Task 2: Old and New Vertex Rules (2 points)

    private static Vector3 OldPoint(Vertex[] vertices, CornerTable table, int vertexIndex)
    {
        // HINT: Obtain 1-ring neighbors using table.VV(vertexIndex).
        // Access vertex positions directly via vertices[idx].Position.
        // Use Weight(neighbors.Length) to get the central vertex weight w.

        var neighs = table.VV(vertexIndex);
        Vector3 pos = new Vector3();
        foreach (int idx in neighs)
        {
            pos += vertices[idx].Position;
        }

        var n = neighs.Length;
        var w = Weight(n);

        pos += w * vertices[vertexIndex].Position;

        pos /= (n + w);

        return pos;
    }

    private static Vector3 NewPoint(Vertex[] vertices, CornerTable table, int corner)
    {
        // TODO: Calculate new point position
        var pos1 = vertices[table.v(corner)].Position;
        var posOp1 = vertices[table.v(table.opposite[corner])].Position;
        var pos3L = vertices[table.v(table.p(corner))].Position;
        var pos3R = vertices[table.v(table.n(corner))].Position;

        Vector3 newPoint = ( pos1 + posOp1 + 3 * pos3L + 3 * pos3R) / 8;

        return newPoint;
    }

    private static float Weight(int n)
    {
        if (_loopWeights.TryGetValue(n, out float cached))
            return cached;

        // TODO: Use the weight formula from the lecture slides
        float w = (64f * n) / ( 40f - MathF.Pow( 3 + 2 * MathF.Cos( 2 * MathF.PI / n ), 2) )  -  n;
        _loopWeights[n] = w;
        return w;
    }

    #endregion

    #region Task 3: Limit Surface Snapping (3 points)

    public static Vertex[] SnapLoop(Vertex[] vertices, int[] indices, CornerTable table)
    {
        // HINT: Allocate result = Vertex.Clone(vertices);
        // For each vertex, compute its limit position using SnapPoint() and assign result[i].Position = ...
        // Recompute smooth normals before returning.
        var result = Vertex.Clone(vertices);

        for (int i = 0; i < result.Length; ++i)
        {
            result[i].Position = SnapPoint(vertices, table, i);
        }

        NormalsGenerator.Apply(result, indices, NormalsMode.Smooth);

        return result;
    }

    private static Vector3 SnapPoint(Vertex[] vertices, CornerTable table, int vertexIndex)
    {
        // HINT: Get neighbors via table.VV(vertexIndex) and weights via SnapWeights(neighbors.Length).
        // weights[0] is for the vertex itself; weights[i + 1] are for its 1-ring neighbors.

        var neighs = table.VV(vertexIndex);
        var weights = SnapWeights(neighs.Length);

        var pos = Vector3.Zero;

        pos += vertices[vertexIndex].Position * (float)weights[0];

        for (int i = 0; i < neighs.Length; ++i)
        {
            pos += vertices[neighs[i]].Position * (float)weights[i+1];
        }

        return pos;
    }

    private static double[] SnapWeights(int n)
    {
        if (_snapWeightsCache.TryGetValue(n, out double[]? cached))
            return cached;

        // HINT: 1. Build subdivision matrix S as Matrix<double>.Build.Dense(n + 1, n + 1)

        var m = n + 1;
        Matrix<double> S = Matrix<double>.Build.Dense(m,m);
        for (int y = 0; y < S.RowCount; ++y)
        {
            for (int x = 0; x < S.ColumnCount; ++x)
            {
                // ROW 0
                if (y == 0)
                {
                    var w = Weight(n);
                    if (x == 0)
                    {
                        S[y, x] = w / (w +n);
                    }
                    else
                    {
                        S[y, x] = 1f / (w + n);
                    }
                    continue;
                }

                // each row i has 3/8 at column 0 and column i, and 1 / 8 at its two cyclic neighbors 1 + (i % n) and 1 + ((i - 2 + n) % n)
                if (x == 0 || x == y)
                {
                    S[y, x] = 3f / 8;
                }
                else if (x == 1 + (y % n) || x == 1 + ((y - 2 + n) % n))
                {
                    S[y, x] = 1 / 8f;
                }
            }
        }


        //       2. Compute eigendecomposition: Evd<double> evd = matrix.Evd();

        Console.Write(S.ToString());
        Evd<double> evd = S.Evd();
        Console.Write(evd.EigenValues);
        Console.Write(evd.EigenVectors.ToString());


        //       3. Locate dominant eigenvalue (lambda = 1), build permutation array, and permute columns:
        //          V.PermuteColumns(new Permutation(perm));
        var idx = evd.EigenValues.Real().MaximumIndex();

        int[] perm = new int[n + 1];
        for (int i = 0; i < perm.Length; ++i)
        {
            perm[i] = i;
        }
        perm[0] = idx;
        perm[idx] = 0;

        var V = evd.EigenVectors;
        V.PermuteColumns(new Permutation(perm));

        //       4. Compute inverse: var inv = V.Inverse();
        var inv = V.Inverse();


        //       5. Extract limit weights: result[i] = V[0, 0] * inv[0, i];
        double[] result = new double[n + 1];
        for (int i = 0; i <  result.Length; ++i)
        {
            result[i] = V[0, 0] * inv[0, i];
        }

        _snapWeightsCache[n] = result;
        return result;
    }

    #endregion
}