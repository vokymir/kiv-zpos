using Core.DataStructures;
using Core.Structures;
using Meta.Numerics.Matrices;
using System;
using System.Numerics;

namespace Core.PostProcessing
{
    public static class LaplacianSmoother
    {
        public static Vertex[] ExplicitSmooth(
            Vertex[] vertices,
            int[] indices,
            IEdgeDataStructure table,
            float lambda,
            LaplacianMode mode)
        {
            return mode switch
            {
                LaplacianMode.Tutte => ExplicitTutteSmooth(vertices, indices, table, lambda),
                LaplacianMode.Cotan => ExplicitCotanSmooth(vertices, indices, table, lambda),
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
        }

        public static Vertex[] ImplicitSmooth(
            Vertex[] vertices,
            int[] indices,
            IEdgeDataStructure table,
            double lambda,
            LaplacianMode mode)
        {
            return mode switch
            {
                LaplacianMode.Tutte => ImplicitTutteSmooth(vertices, indices, table, lambda),
                LaplacianMode.Cotan => ImplicitCotanSmooth(vertices, indices, table, lambda),
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
        }

        public static Vertex[] ExplicitTutteSmooth(Vertex[] vertices, int[] indices, IEdgeDataStructure table, float lambda)
        {
            Vertex[] result = Vertex.Clone(vertices);
            Vector3[] laplacians = LaplacianGenerator.ComputeTutte(vertices, table);

            for (int i = 0; i < result.Length; i++)
            {
                result[i].Position = vertices[i].Position + lambda * laplacians[i];
            }

            NormalsGenerator.Apply(result, indices, NormalsMode.Smooth);
            return result;
        }

        public static Vertex[] ExplicitCotanSmooth(Vertex[] vertices, int[] indices, IEdgeDataStructure table, float lambda)
        {
            Vertex[] result = Vertex.Clone(vertices);
            Vector3[] laplacians = LaplacianGenerator.ComputeCotan(vertices, indices, table);

            for (int i = 0; i < result.Length; i++)
            {
                result[i].Position = vertices[i].Position + lambda * laplacians[i];
            }

            NormalsGenerator.Apply(result, indices, NormalsMode.Smooth);
            return result;
        }

        public static Vertex[] ImplicitTutteSmooth(Vertex[] vertices, int[] indices, IEdgeDataStructure table, double lambda)
        {
            SparseSquareMatrix A = BuildImplicitTutteMatrix(vertices.Length, table, lambda);
            return SolveImplicitSystem(vertices, indices, A);
        }

        public static Vertex[] ImplicitCotanSmooth(Vertex[] vertices, int[] indices, IEdgeDataStructure table, double lambda)
        {
            SparseSquareMatrix A = BuildImplicitCotanMatrix(vertices, indices, table, lambda);
            return SolveImplicitSystem(vertices, indices, A);
        }

        public static SparseSquareMatrix BuildImplicitTutteMatrix(int vertexCount, IEdgeDataStructure table, double lambda)
        {
            SparseSquareMatrix matrix = new SparseSquareMatrix(vertexCount);

            for (int i = 0; i < vertexCount; i++)
            {
                int[] neighbors = table.VV(i);
                int n = neighbors.Length;

                if (n == 0)
                {
                    matrix[i, i] = 1.0;
                    continue;
                }

                matrix[i, i] = 1.0 + lambda;

                for (int j = 0; j < n; j++)
                {
                    matrix[i, neighbors[j]] = -lambda / n;
                }
            }

            return matrix;
        }

        public static SparseSquareMatrix BuildImplicitCotanMatrix(Vertex[] vertices, int[] indices, IEdgeDataStructure table, double lambda)
        {
            int vertexCount = vertices.Length;
            float[] incidentAreas = LaplacianGenerator.PrepareAreas(vertices, indices);
            SparseSquareMatrix matrix = new SparseSquareMatrix(vertexCount);

            for (int i = 0; i < vertexCount; i++)
            {
                if (table.IsOnBorder(i) || incidentAreas[i] <= 1e-12f)
                {
                    matrix[i, i] = 1.0;
                    continue;
                }

                int[] neighbors = table.VV(i);
                float[] weights = LaplacianGenerator.ComputeCotanWeights(vertices, i, neighbors, out float weightSum);
                double area = incidentAreas[i];

                matrix[i, i] = 1.0 + lambda * (weightSum / area);

                for (int j = 0; j < neighbors.Length; j++)
                {
                    matrix[i, neighbors[j]] = -lambda * (weights[j] / area);
                }
            }

            return matrix;
        }

        public static Vertex[] SolveImplicitSystem(Vertex[] vertices, int[] indices, SparseSquareMatrix A)
        {
            int n = vertices.Length;
            Vertex[] result = Vertex.Clone(vertices);

            ColumnVector rhsX = new ColumnVector(n);
            ColumnVector rhsY = new ColumnVector(n);
            ColumnVector rhsZ = new ColumnVector(n);

            for (int i = 0; i < n; i++)
            {
                Vector3 pos = vertices[i].Position;
                rhsX[i] = pos.X;
                rhsY[i] = pos.Y;
                rhsZ[i] = pos.Z;
            }

            ColumnVector solX = A.Solve(rhsX);
            ColumnVector solY = A.Solve(rhsY);
            ColumnVector solZ = A.Solve(rhsZ);

            for (int i = 0; i < n; i++)
            {
                result[i].Position = new Vector3((float)solX[i], (float)solY[i], (float)solZ[i]);
            }

            NormalsGenerator.Apply(result, indices, NormalsMode.Smooth);
            return result;
        }
    }
}