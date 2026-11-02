using Core.DataStructures;
using Core.Structures;
using System;
using System.Numerics;

namespace Core.PostProcessing
{
    public enum LaplacianMode
    {
        Tutte,
        Cotan
    }

    public static class LaplacianGenerator
    {
        public static Vector3[] Compute(Vertex[] vertices, int[] indices, IEdgeDataStructure table, LaplacianMode mode)
        {
            return mode switch
            {
                LaplacianMode.Tutte => ComputeTutte(vertices, table),
                LaplacianMode.Cotan => ComputeCotan(vertices, indices, table),
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };
        }

        public static Vector3[] ComputeTutte(Vertex[] vertices, IEdgeDataStructure table)
        {
            Vector3[] result = new Vector3[vertices.Length];

            for (int i = 0; i < vertices.Length; i++)
            {
                int[] neighbors = table.VV(i);
                int n = neighbors.Length;
                if (n == 0) continue;

                Vector3 sum = Vector3.Zero;
                for (int j = 0; j < n; j++)
                {
                    sum += vertices[neighbors[j]].Position;
                }

                result[i] = (sum / n) - vertices[i].Position;
            }

            return result;
        }

        public static Vector3[] ComputeCotan(Vertex[] vertices, int[] indices, IEdgeDataStructure table)
        {
            float[] incidentAreas = PrepareAreas(vertices, indices);
            return ComputeCotan(vertices, table, incidentAreas);
        }

        public static Vector3[] ComputeCotan(Vertex[] vertices, IEdgeDataStructure table, float[] incidentAreas)
        {
            Vector3[] result = new Vector3[vertices.Length];

            for (int vertex = 0; vertex < vertices.Length; vertex++)
            {
                if (table.IsOnBorder(vertex) || incidentAreas[vertex] <= 1e-12f)
                    continue;

                int[] neighbors = table.VV(vertex);
                float[] weights = ComputeCotanWeights(vertices, vertex, neighbors, out float weightSum);

                Vector3 sum = Vector3.Zero;
                for (int i = 0; i < neighbors.Length; i++)
                {
                    sum += weights[i] * vertices[neighbors[i]].Position;
                }

                result[vertex] = (sum - weightSum * vertices[vertex].Position) / incidentAreas[vertex];
            }

            return result;
        }

        public static float[] ComputeCotanWeights(Vertex[] vertices, int vertex, int[] neighbors, out float weightSum)
        {
            int n = neighbors.Length;
            float[] weights = new float[n];
            weightSum = 0f;

            Vector3 vPos = vertices[vertex].Position;

            for (int i = 0; i < n; i++)
            {
                Vector3 nPos = vertices[neighbors[i]].Position;
                Vector3 nextPos = vertices[neighbors[(i + 1) % n]].Position;
                Vector3 prevPos = vertices[neighbors[(i + n - 1) % n]].Position;

                float alpha = Mathematics.Angle(nPos, nextPos, vPos);
                float beta = Mathematics.Angle(nPos, prevPos, vPos);

                float weight = 0.5f * (1f / MathF.Tan(alpha) + 1f / MathF.Tan(beta));
                weights[i] = weight;
                weightSum += weight;
            }

            return weights;
        }

        public static float[] PrepareAreas(Vertex[] vertices, int[] indices)
        {
            float[] incidentAreas = new float[vertices.Length];
            int faceCount = indices.Length / 3;

            for (int f = 0; f < faceCount; f++)
            {
                int i1 = indices[f * 3];
                int i2 = indices[f * 3 + 1];
                int i3 = indices[f * 3 + 2];

                Vector3 v1 = vertices[i1].Position;
                Vector3 v2 = vertices[i2].Position;
                Vector3 v3 = vertices[i3].Position;

                Vector3 v1v2Half = 0.5f * (v1 + v2);
                Vector3 v2v3Half = 0.5f * (v2 + v3);
                Vector3 v3v1Half = 0.5f * (v3 + v1);

                float aa = (v2 - v3).LengthSquared();
                float bb = (v3 - v1).LengthSquared();
                float cc = (v2 - v1).LengthSquared();

                float d1 = aa * (bb + cc - aa);
                float d2 = bb * (cc + aa - bb);
                float d3 = cc * (aa + bb - cc);

                float dSum = d1 + d2 + d3;
                if (MathF.Abs(dSum) <= 1e-12f) continue;

                d1 /= dSum;
                d2 /= dSum;
                d3 /= dSum;

                if (d1 < 0f)
                {
                    incidentAreas[i1] += Mathematics.TriangleArea(v3v1Half, v1, v2v3Half) + Mathematics.TriangleArea(v1v2Half, v1, v2v3Half);
                    incidentAreas[i2] += Mathematics.TriangleArea(v1v2Half, v2, v2v3Half);
                    incidentAreas[i3] += Mathematics.TriangleArea(v2v3Half, v3, v3v1Half);
                }
                else if (d2 < 0f)
                {
                    incidentAreas[i1] += Mathematics.TriangleArea(v3v1Half, v1, v1v2Half);
                    incidentAreas[i2] += Mathematics.TriangleArea(v1v2Half, v2, v3v1Half) + Mathematics.TriangleArea(v3v1Half, v2, v2v3Half);
                    incidentAreas[i3] += Mathematics.TriangleArea(v2v3Half, v3, v3v1Half);
                }
                else if (d3 < 0f)
                {
                    incidentAreas[i1] += Mathematics.TriangleArea(v3v1Half, v1, v1v2Half);
                    incidentAreas[i2] += Mathematics.TriangleArea(v1v2Half, v2, v2v3Half);
                    incidentAreas[i3] += Mathematics.TriangleArea(v2v3Half, v3, v1v2Half) + Mathematics.TriangleArea(v1v2Half, v3, v3v1Half);
                }
                else
                {
                    Vector3 circumcenter = d1 * v1 + d2 * v2 + d3 * v3;

                    incidentAreas[i1] += Mathematics.TriangleArea(v3v1Half, v1, circumcenter) + Mathematics.TriangleArea(v1v2Half, v1, circumcenter);
                    incidentAreas[i2] += Mathematics.TriangleArea(v1v2Half, v2, circumcenter) + Mathematics.TriangleArea(v2v3Half, v2, circumcenter);
                    incidentAreas[i3] += Mathematics.TriangleArea(v2v3Half, v3, circumcenter) + Mathematics.TriangleArea(v3v1Half, v3, circumcenter);
                }
            }

            return incidentAreas;
        }
    }
}