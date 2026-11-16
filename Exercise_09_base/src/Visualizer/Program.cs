using Core.DataStructures;
using Core.IO;
using Core.PostProcessing;
using Core.Structures;
using Meta.Numerics.Matrices;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Visualizer;

namespace MeshParameterization
{
    public class Program
    {
        static void Main(string[] args)
        {
            IMeshImporter importer = MeshImporterFactory.Resolve("obj");
            // Note: The input mesh should be topologically equivalent to a disc (having a single continuous border)
            MeshData data = importer.Load("Assets/Models/lion2.obj");

            Vertex[] origVertices = LaplacianSmoother.CloneVertices(data.Vertices);
            int[] origIndices = data.Indices;

            // Parameterize the mesh (maps border to a unit circle and flattens inner vertices)
            (Vertex[] paramVertices, int[] paramIndices) = Parameterize(origVertices, origIndices);

            // Recalculate normals for smooth shading
            NormalsGenerator.Apply(origVertices, origIndices, NormalsMode.Smooth);
            // (Normals for paramVertices should be recalculated inside the Parameterize method)

            VisualizerSession.Run(session =>
            {
                session.SetAnimation(
                    [origVertices, paramVertices],
                    [origIndices, paramIndices]
                );
            });
        }

        public static (Vertex[] Vertices, int[] Indices) Parameterize(Vertex[] vertices, int[] indices)
        {
            CornerTable table = new CornerTable(indices, vertices.Length);
            Vertex[] result = LaplacianSmoother.CloneVertices(vertices);

            List<int> border = new List<int>();
            Dictionary<int, int> unknownIndices = new Dictionary<int, int>();
            List<int> unknowns = new List<int>();

            // TODO: 1. Determine the border of the mesh and populate the 'border' list
            GetBorderVertices(table, result.Length, border);

            // TODO: 2. Classify inner vertices as unknowns and populate 'unknowns' and 'unknownIndices'
            PopulateUnknowns(table, result.Length, unknowns, unknownIndices);

            // TODO: 3. Map border vertices to unit circle
            MapBorderOnUnitCircle(result, border);

            // TODO: 4. Build a system of equations and right-hand sides using SparseSquareMatrix and ColumnVector
            var UxU = BuildUwUmatrix(vertices, unknowns, table, unknownIndices);
            var (u, v) = BuildRHS(result, vertices, unknowns, border, table, unknownIndices);

            // TODO: 5. Solve the system for U and V coordinates and assign them to the result vertices
            var res_U = UxU.Solve(u);
            var res_V = UxU.Solve(v);

            for (int i = 0; i < res_U.RowCount; ++i) {
                var idx = unknowns[i];
                result[idx].Position = new Vector3((float)res_U[i], (float)res_V[i], 0); 
            }

            // TODO: 6. Recalculate normals for the flattened mesh using NormalsGenerator
            NormalsGenerator.Apply(result, indices, NormalsMode.Smooth);

            return (result, indices);
        }

        public static (ColumnVector u, ColumnVector v) BuildRHS(Vertex[] result, Vertex[] vertices, List<int> unknowns, List<int> border, CornerTable table, Dictionary<int,int>unknownIndices) {
            var dim = unknowns.Count;
            var u = new ColumnVector(dim); var v = new ColumnVector(dim);

            for (int i = 0; i < dim; ++i)
            {
                var vtx = unknowns[i];
                var neighs = table.VV(vtx);

                float[] weights = LaplacianGenerator.ComputeCotanWeights(vertices, vtx, neighs, out float weightSum);
                for(int j = 0; j < neighs.Length; ++j)
                {
                    var n = neighs[j];
                    if (!table.IsOnBorder(n)) { continue; }

                    var wei = weights[j];

                    var lambda = (wei / weightSum);

                    var pos = result[n].Position;

                    u[i] += lambda * pos.X;
                    v[i] += lambda * pos.Y;
                }

                u[i] *= -1;
                v[i] *= -1;
            }

            return (u, v);
        }

        public static SparseSquareMatrix BuildUwUmatrix(Vertex[] vertices, List<int> unknowns, IEdgeDataStructure table, Dictionary<int,int> unknownIndices)
        {
            int unknownsCount = unknowns.Count;
            SparseSquareMatrix matrix = new SparseSquareMatrix(unknownsCount);

            for (int i = 0; i < unknownsCount; i++)
            {
                int unknown_vtx = unknowns[i];

                int[] neighbors = table.VV(unknown_vtx);
                float[] weights = LaplacianGenerator.ComputeCotanWeights(vertices, unknown_vtx, neighbors, out float weightSum);

                matrix[i, i] = -1.0;
                for (int j = 0; j < neighbors.Length; j++)
                {
                    var neigh = neighbors[j];
                    var wei = weights[j];
                    if (table.IsOnBorder(neigh)) { continue; }

                    var nei = unknownIndices[neigh];
                    matrix[i, nei] = (wei / weightSum);
                }
            }

            return matrix;
        }

        private static void GetBorderVertices(CornerTable table, int vertexCount, List<int> border)
        {
            // TODO: Identify which vertices are located on the border and extract them in order
            int initial_border_vtx = 0;
            while (!table.IsOnBorder(initial_border_vtx))
            {
                initial_border_vtx++;
            }
            int curr = initial_border_vtx;
            do { 
                border.Add(curr);
                curr = NextVertexOnBorder(table, curr);
            } while (curr != initial_border_vtx);
        }

        private static void PopulateUnknowns(CornerTable table, int vertexCount, List<int> unknowns, Dictionary<int, int> unknownIndices)
        {
            for (int i = 0; i < vertexCount; ++i)
            {
                if (table.IsOnBorder(i)) { continue; }
                unknownIndices.Add(i, unknowns.Count);
                unknowns.Add(i);
            }
        }

        public static int NextVertexOnBorder(CornerTable table, int vertex)
        {
            // TODO: Provide the subsequent border vertex for a given border vertex
            return table.nextVertexOnBorder(vertex);
        }

        private static void MapBorderOnUnitCircle(Vertex[] vertices, List<int> border)
        {
            // TODO: Calculate angle steps and map the known border vertices to a 2D unit circle
            var a = 2 * MathF.PI / border.Count;
            var r = 1f;
            for (int i = 0; i < border.Count; ++i)
            {
                var angle = i * a;
                vertices[border[i]].Position = new Vector3(r * MathF.Sin(angle),r * MathF.Cos(angle),0);
            }
        }
    }
}