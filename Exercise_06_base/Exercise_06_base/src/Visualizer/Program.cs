using Core.DataStructures;
using Core.IO;
using Core.PostProcessing;
using Core.Structures;
using Meta.Numerics.Matrices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using Visualizer;

class Program
{
    static void Main(string[] args)
    {
        // 1. Load Mesh Data
        IMeshImporter importer = MeshImporterFactory.Resolve("obj");
        MeshData data = importer.Load("Assets/Models/lion.obj");

        Vertex[] baseVertices = data.Vertices;
        int[] indices = data.Indices;

        NormalsGenerator.Apply(baseVertices, indices, NormalsMode.Smooth);

        // 2. Build Topology
        CornerTable table = new CornerTable(indices, baseVertices.Length);

        // 3. Build Animation Sequence
        List<Vertex[]> frames = new List<Vertex[]>();
        frames.Add(CloneVertices(baseVertices));

        // TODO: Generate smoothing frames and add them to 'frames'.
        // Example for explicit smoothing:
        // for (int step = 0; step < 40; step++)
        // {
        //     Vertex[] nextFrame = ExplicitTutteSmooth(frames[^1], indices, table, 0.2f);
        //     frames.Add(nextFrame);
        // }
        //
        // Example for implicit smoothing:
        // frames.Add(ImplicitCotanSmooth(baseVertices, indices, table, 0.05));

        var lambda_tutte_explicit = 0.2f;
        var lambda_cotan_explicit = 0.0002f;
        var lambda_tutte_implicit = 1f;
        var lambda_cotan_implicit = 0.05f;
        var n_frames = 40; // frames per smoothing alg

        for (int step = 0; step < n_frames; step++)
        {
            Vertex[] nextFrame = ExplicitTutteSmooth(frames[^1], indices, table, lambda_tutte_explicit);
            frames.Add(nextFrame);
        }

        frames.Add(CloneVertices(baseVertices));

        for (int step = 0; step < n_frames; step++)
        {
            Vertex[] nextFrame = ExplicitCotanSmooth(frames[^1], indices, table, lambda_cotan_explicit);
            frames.Add(nextFrame);
        }

        frames.Add(CloneVertices(baseVertices));

        frames.Add(ImplicitTutteSmooth(baseVertices, indices, table, lambda_tutte_implicit));

        frames.Add(CloneVertices(baseVertices));

        frames.Add(ImplicitCotanSmooth(baseVertices, indices, table, lambda_cotan_implicit));


        // Prepare frame arrays for the visualizer
        Vertex[][] animFrames = frames.ToArray();
        int[][] animIndices = new int[animFrames.Length][];
        for (int i = 0; i < animIndices.Length; i++)
        {
            animIndices[i] = indices;
        }

        // 4. Launch Visualizer
        VisualizerSession.Run(session =>
        {
            session.SetAnimation(animFrames, animIndices);
        });
    }

    private static Vertex[] ExplicitTutteSmooth(Vertex[] vertices, int[] indices, IEdgeDataStructure table, float lambda)
    {
        Vertex[] result = CloneVertices(vertices);

        // TODO: Compute the Tutte Laplacian (using LaplacianGenerator.ComputeTutte)
        // and update vertex positions: p_new = p_old + lambda * L(p_old).

        Vector3[] L = LaplacianGenerator.ComputeTutte(vertices, table);

        for (int i = 0; i < result.Length; ++i) {
            var pos = result[i].Channel("position");
            
            pos[0] = pos[0] + lambda * L[i].X;
            pos[1] = pos[1] + lambda * L[i].Y;
            pos[2] = pos[2] + lambda * L[i].Z;

            result[i].Channels["position"] = pos;
        }

        NormalsGenerator.Apply(result, indices, NormalsMode.Smooth);
        return result;
    }

    private static Vertex[] ExplicitCotanSmooth(Vertex[] vertices, int[] indices, IEdgeDataStructure table, float lambda)
    {
        Vertex[] result = CloneVertices(vertices);

        // TODO: Compute the Cotan Laplacian (using LaplacianGenerator.ComputeCotan)
        // and update vertex positions: p_new = p_old + lambda * L(p_old).
        
        Vector3[] L = LaplacianGenerator.ComputeCotan(vertices, indices, table);

        for (int i = 0; i < result.Length; ++i) {
            var pos = result[i].Channel("position");
            
            pos[0] = pos[0] + lambda * L[i].X;
            pos[1] = pos[1] + lambda * L[i].Y;
            pos[2] = pos[2] + lambda * L[i].Z;

            result[i].Channels["position"] = pos;
        }


        NormalsGenerator.Apply(result, indices, NormalsMode.Smooth);
        return result;
    }

    private static Vertex[] ImplicitTutteSmooth(Vertex[] vertices, int[] indices, IEdgeDataStructure table, double lambda)
    {
        Vertex[] result = CloneVertices(vertices);

        // TODO:
        // 1. Construct a SparseSquareMatrix A = (I - lambda * L) of size vertices.Length.
        // 2. Construct ColumnVector instances for the X, Y, and Z coordinates of 'vertices'.
        // 3. Solve the linear systems using A.Solve(...) and write the new positions to 'result'.

        var n = vertices.Length;

        SparseSquareMatrix A = new SparseSquareMatrix(n);
        SparseSquareMatrix L = new SparseSquareMatrix(n);

        for (int row = 0; row < n; ++row)
        {
            var neighs = table.VV(row);
            var degree = neighs.Length;

            L[row, row] = -1f;
            A[row, row] = 1f - lambda * L[row,row];

            foreach (var neigh in neighs)
            {
                L[row, neigh] = 1f / degree;
                A[row, neigh] -= lambda * L[row, neigh];
            }
        }

        ColumnVector bX = new ColumnVector(n);
        ColumnVector bY = new ColumnVector(n);
        ColumnVector bZ = new ColumnVector(n);

        for (int i = 0; i < n; ++i) {
            var pos = vertices[i].Channel("position");
            bX[i] = pos[0];
            bY[i] = pos[1];
            bZ[i] = pos[2];
        }

        var solvedX = A.Solve(bX);
        var solvedY = A.Solve(bY);
        var solvedZ = A.Solve(bZ);

        for (int i = 0; i < n; ++i) {
            result[i].Channels["position"][0] = (float)solvedX[i];
            result[i].Channels["position"][1] = (float)solvedY[i];
            result[i].Channels["position"][2] = (float)solvedZ[i];
        }

        NormalsGenerator.Apply(result, indices, NormalsMode.Smooth);
        return result;
    }

    private static Vertex[] ImplicitCotanSmooth(Vertex[] vertices, int[] indices, IEdgeDataStructure table, double lambda)
    {
        Vertex[] result = CloneVertices(vertices);

        // TODO:
        // 1. Precompute mixed Voronoi areas using LaplacianGenerator.PrepareAreas(vertices, indices).
        // 2. Construct a SparseSquareMatrix A = (I - lambda * L) using cotangent weights.
        // 3. Construct ColumnVector instances for the X, Y, and Z coordinates of 'vertices'.
        // 4. Solve the linear systems using A.Solve(...) and write the new positions to 'result'.

        var n = vertices.Length;

        var mixedVoronoi = LaplacianGenerator.PrepareAreas(vertices, indices);
        SparseSquareMatrix A = new SparseSquareMatrix(n);
        SparseSquareMatrix L = new SparseSquareMatrix(n);

        for (int row = 0; row < n; ++row)
        {
            var area = mixedVoronoi[row];
            var neighs = table.VV(row);
            var degree = neighs.Length;
            var w = LaplacianGenerator.ComputeCotanWeights(vertices, row, neighs, out float w_sum);

            L[row, row] = - w_sum / area;
            A[row, row] = 1;

            for (int i = 0; i < neighs.Length; ++i)
            {
                var neigh = neighs[i];
                var wei = w[i];
                L[row, neigh] =   wei / area;
            }
        }

        for (int row = 0; row < n; ++row)
        {
            for (int col = 0; col < n; ++col)
            {
                A[row, col] -= lambda * L[row,col];
            }
        }

        ColumnVector bX = new ColumnVector(n);
        ColumnVector bY = new ColumnVector(n);
        ColumnVector bZ = new ColumnVector(n);

        for (int i = 0; i < n; ++i) {
            var pos = vertices[i].Channel("position");
            bX[i] = pos[0];
            bY[i] = pos[1];
            bZ[i] = pos[2];
        }

        var solvedX = A.Solve(bX);
        var solvedY = A.Solve(bY);
        var solvedZ = A.Solve(bZ);

        for (int i = 0; i < n; ++i) {
            result[i].Channels["position"][0] = (float)solvedX[i];
            result[i].Channels["position"][1] = (float)solvedY[i];
            result[i].Channels["position"][2] = (float)solvedZ[i];
        }

        NormalsGenerator.Apply(result, indices, NormalsMode.Smooth);
        return result;
    }

    /// <summary>
    /// Creates a deep copy of a Vertex array so changes to one frame do not mutate previous frames.
    /// </summary>
    private static Vertex[] CloneVertices(Vertex[] source)
    {
        Vertex[] copy = new Vertex[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            copy[i] = source[i].Clone();
        }
        return copy;
    }
}