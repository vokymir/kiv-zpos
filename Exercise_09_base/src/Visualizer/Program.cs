using Core.DataStructures;
using Core.IO;
using Core.PostProcessing;
using Core.Structures;
using Meta.Numerics.Matrices;
using System;
using System.Collections.Generic;
using System.Numerics;
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

            // TODO: 2. Classify inner vertices as unknowns and populate 'unknowns' and 'unknownIndices'

            // TODO: 3. Map border vertices to unit circle

            // TODO: 4. Build a system of equations and right-hand sides using SparseSquareMatrix and ColumnVector

            // TODO: 5. Solve the system for U and V coordinates and assign them to the result vertices

            // TODO: 6. Recalculate normals for the flattened mesh using NormalsGenerator

            throw new NotImplementedException("Parameterization logic needs to be implemented.");

            // return (result, indices);
        }

        private static void GetBorderVertices(CornerTable table, int vertexCount, Dictionary<int, int> unknownIndices, List<int> border)
        {
            // TODO: Identify which vertices are located on the border and extract them in order
            throw new NotImplementedException();
        }

        public static int NextVertexOnBorder(CornerTable table, int vertex)
        {
            // TODO: Provide the subsequent border vertex for a given border vertex
            throw new NotImplementedException();
        }

        private static void MapBorderOnUnitCircle(Vertex[] vertices, List<int> border)
        {
            // TODO: Calculate angle steps and map the known border vertices to a 2D unit circle
            throw new NotImplementedException();
        }
    }
}