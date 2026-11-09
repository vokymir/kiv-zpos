using Core.DataStructures;
using Core.Structures;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Core.PostProcessing
{
    public static class LoopSubdivision
    {
        private static readonly Dictionary<int, float> _loopWeights = [];
        private static readonly Dictionary<int, double[]> _snapWeightsCache = [];

        public static (Vertex[] Vertices, int[] Indices) Loop(Vertex[] vertices, int[] indices, CornerTable table)
        {
            int faceCount = indices.Length / 3;
            Vertex[] newVertices = new Vertex[vertices.Length + (faceCount * 3) / 2];

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 pOld = OldPoint(vertices, table, i);
                newVertices[i] = Vertex.FromPositionNormal(pOld.X, pOld.Y, pOld.Z);
            }

            int index = vertices.Length;
            int[] edgeIndices = new int[table.opposite.Length];
            for (int i = 0; i < table.opposite.Length; i++)
            {
                if (i < table.opposite[i])
                {
                    Vector3 pNew = NewPoint(vertices, table, i);
                    newVertices[index] = Vertex.FromPositionNormal(pNew.X, pNew.Y, pNew.Z);
                    edgeIndices[i] = edgeIndices[table.opposite[i]] = index++;
                }
            }

            int[] newIndices = new int[indices.Length * 4];
            for (int i = 0; i < faceCount; i++)
            {
                int c1 = 3 * i, c2 = 3 * i + 1, c3 = 3 * i + 2;
                int v1 = indices[c1], v2 = indices[c2], v3 = indices[c3];
                int e1 = edgeIndices[c1], e2 = edgeIndices[c2], e3 = edgeIndices[c3];

                int baseIdx = 12 * i;

                newIndices[baseIdx] = v1;
                newIndices[baseIdx + 1] = e3;
                newIndices[baseIdx + 2] = e2;

                newIndices[baseIdx + 3] = v3;
                newIndices[baseIdx + 4] = e2;
                newIndices[baseIdx + 5] = e1;

                newIndices[baseIdx + 6] = v2;
                newIndices[baseIdx + 7] = e1;
                newIndices[baseIdx + 8] = e3;

                newIndices[baseIdx + 9] = e1;
                newIndices[baseIdx + 10] = e2;
                newIndices[baseIdx + 11] = e3;
            }

            NormalsGenerator.Apply(newVertices, newIndices, NormalsMode.Smooth);
            return (newVertices, newIndices);
        }

        private static Vector3 OldPoint(Vertex[] vertices, CornerTable table, int vertexIndex)
        {
            int[] neighbors = table.VV(vertexIndex);
            Vector3 sum = Vector3.Zero;
            for (int i = 0; i < neighbors.Length; i++)
                sum += vertices[neighbors[i]].Position;

            float w = Weight(neighbors.Length);
            return (sum + w * vertices[vertexIndex].Position) / (w + neighbors.Length);
        }

        private static Vector3 NewPoint(Vertex[] vertices, CornerTable table, int corner)
        {
            Vector3 pLeft = vertices[table.v(table.p(corner))].Position;
            Vector3 pRight = vertices[table.v(table.n(corner))].Position;
            Vector3 pBottom = vertices[table.v(corner)].Position;
            Vector3 pTop = vertices[table.v(table.opposite[corner])].Position;
            return (1f / 8f) * (pBottom + pTop) + (3f / 8f) * (pLeft + pRight);
        }

        private static float Weight(int n)
        {
            if (_loopWeights.TryGetValue(n, out float cached)) return cached;
            float cos = MathF.Cos(2f * MathF.PI / n);
            float term = 3f + 2f * cos;
            return _loopWeights[n] = (64f * n) / (40f - term * term) - n;
        }

        public static Vertex[] SnapLoop(Vertex[] vertices, int[] indices, CornerTable table)
        {
            Vertex[] result = LaplacianSmoother.CloneVertices(vertices);
            for (int i = 0; i < vertices.Length; i++)
                result[i].Position = SnapPoint(vertices, table, i);

            NormalsGenerator.Apply(result, indices, NormalsMode.Smooth);
            return result;
        }

        private static Vector3 SnapPoint(Vertex[] vertices, CornerTable table, int vertexIndex)
        {
            int[] neighbors = table.VV(vertexIndex);
            double[] weights = SnapWeights(neighbors.Length);
            Vector3 result = (float)weights[0] * vertices[vertexIndex].Position;
            for (int i = 0; i < neighbors.Length; i++)
                result += (float)weights[i + 1] * vertices[neighbors[i]].Position;
            return result;
        }

        private static double[] SnapWeights(int n)
        {
            if (_snapWeightsCache.TryGetValue(n, out double[]? cached)) return cached;
            Matrix<double> matrix = Matrix<double>.Build.Dense(n + 1, n + 1);
            double w = Weight(n);
            matrix[0, 0] = w / (w + n);

            for (int i = 1; i <= n; i++)
            {
                matrix[0, i] = 1.0 / (w + n);
                matrix[i, 0] = matrix[i, i] = 3.0 / 8.0;
                matrix[i, 1 + (i % n)] = matrix[i, 1 + ((i - 2 + n) % n)] = 1.0 / 8.0;
            }

            Evd<double> evd = matrix.Evd();
            double max = evd.EigenValues[0].Real;
            int maxIndex = 0;
            int[] perm = new int[n + 1];

            for (int i = 0; i < n; i++)
            {
                perm[i + 1] = i + 1;
                if (evd.EigenValues[i + 1].Real > max)
                {
                    max = evd.EigenValues[i + 1].Real;
                    maxIndex = i + 1;
                }
            }

            perm[0] = maxIndex;
            perm[maxIndex] = 0;
            Matrix<double> V = evd.EigenVectors.Clone();
            V.PermuteColumns(new MathNet.Numerics.Permutation(perm));
            Matrix<double> inv = V.Inverse();

            double[] result = new double[n + 1];
            for (int i = 0; i < result.Length; i++) result[i] = V[0, 0] * inv[0, i];
            return _snapWeightsCache[n] = result;
        }
    }
}