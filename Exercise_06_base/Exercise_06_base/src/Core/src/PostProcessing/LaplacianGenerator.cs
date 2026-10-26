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

        /// <summary>
        /// Computes the Combinatorial (Tutte / Uniform) Laplacian for all vertices.
        /// </summary>
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
                    sum += GetPosition(vertices[neighbors[j]]);
                }

                result[i] = (sum / n) - GetPosition(vertices[i]);
            }

            return result;
        }

        /// <summary>
        /// Computes the Cotangent Laplacian for all vertices using mixed Voronoi areas.
        /// </summary>
        public static Vector3[] ComputeCotan(Vertex[] vertices, int[] indices, IEdgeDataStructure table)
        {
            float[] incidentAreas = PrepareAreas(vertices, indices);
            return ComputeCotan(vertices, table, incidentAreas);
        }

        /// <summary>
        /// Computes the Cotangent Laplacian using precomputed mixed Voronoi vertex areas.
        /// </summary>
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
                    sum += weights[i] * GetPosition(vertices[neighbors[i]]);
                }

                Vector3 vPos = GetPosition(vertices[vertex]);
                result[vertex] = (sum - weightSum * vPos) / incidentAreas[vertex];
            }

            return result;
        }

        /// <summary>
        /// Computes the cotangent weights w_ij = 0.5 * (cot(alpha) + cot(beta)) for the ordered 1-ring neighbors of a vertex.
        /// </summary>
        public static float[] ComputeCotanWeights(Vertex[] vertices, int vertex, int[] neighbors, out float weightSum)
        {
            int n = neighbors.Length;
            float[] weights = new float[n];
            weightSum = 0f;

            Vector3 vPos = GetPosition(vertices[vertex]);

            for (int i = 0; i < n; i++)
            {
                Vector3 nPos = GetPosition(vertices[neighbors[i]]);
                Vector3 nextPos = GetPosition(vertices[neighbors[(i + 1) % n]]);
                Vector3 prevPos = GetPosition(vertices[neighbors[(i + n - 1) % n]]);

                float alpha = Angle(nPos, nextPos, vPos);
                float beta = Angle(nPos, prevPos, vPos);

                float weight = 0.5f * (1f / MathF.Tan(alpha) + 1f / MathF.Tan(beta));
                weights[i] = weight;
                weightSum += weight;
            }

            return weights;
        }

        /// <summary>
        /// Precomputes the mixed Voronoi / circumcentric area of influence A_i for each vertex.
        /// </summary>
        public static float[] PrepareAreas(Vertex[] vertices, int[] indices)
        {
            float[] incidentAreas = new float[vertices.Length];
            int faceCount = indices.Length / 3;

            for (int f = 0; f < faceCount; f++)
            {
                int i1 = indices[f * 3];
                int i2 = indices[f * 3 + 1];
                int i3 = indices[f * 3 + 2];

                Vector3 v1 = GetPosition(vertices[i1]);
                Vector3 v2 = GetPosition(vertices[i2]);
                Vector3 v3 = GetPosition(vertices[i3]);

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
                    incidentAreas[i1] += TriangleArea(v3v1Half, v1, v2v3Half) + TriangleArea(v1v2Half, v1, v2v3Half);
                    incidentAreas[i2] += TriangleArea(v1v2Half, v2, v2v3Half);
                    incidentAreas[i3] += TriangleArea(v2v3Half, v3, v3v1Half);
                }
                else if (d2 < 0f)
                {
                    incidentAreas[i1] += TriangleArea(v3v1Half, v1, v1v2Half);
                    incidentAreas[i2] += TriangleArea(v1v2Half, v2, v3v1Half) + TriangleArea(v3v1Half, v2, v2v3Half);
                    incidentAreas[i3] += TriangleArea(v2v3Half, v3, v3v1Half);
                }
                else if (d3 < 0f)
                {
                    incidentAreas[i1] += TriangleArea(v3v1Half, v1, v1v2Half);
                    incidentAreas[i2] += TriangleArea(v1v2Half, v2, v2v3Half);
                    incidentAreas[i3] += TriangleArea(v2v3Half, v3, v1v2Half) + TriangleArea(v1v2Half, v3, v3v1Half);
                }
                else
                {
                    Vector3 circumcenter = d1 * v1 + d2 * v2 + d3 * v3;

                    incidentAreas[i1] += TriangleArea(v3v1Half, v1, circumcenter) + TriangleArea(v1v2Half, v1, circumcenter);
                    incidentAreas[i2] += TriangleArea(v1v2Half, v2, circumcenter) + TriangleArea(v2v3Half, v2, circumcenter);
                    incidentAreas[i3] += TriangleArea(v2v3Half, v3, circumcenter) + TriangleArea(v3v1Half, v3, circumcenter);
                }
            }

            return incidentAreas;
        }

        public static float Angle(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            Vector3 v1 = p1 - p2;
            Vector3 v2 = p3 - p2;
            float denom = v1.Length() * v2.Length();
            if (denom <= 1e-12f) return 0f;

            float cosine = Math.Clamp(Vector3.Dot(v1, v2) / denom, -1f, 1f);
            return MathF.Acos(cosine);
        }

        public static float TriangleArea(Vector3 a, Vector3 b, Vector3 c)
        {
            return Vector3.Cross(a - b, c - b).Length() * 0.5f;
        }

        public static Vector3 GetPosition(Vertex v)
        {
            float[] pos = v.Channels["position"];
            return new Vector3(pos[0], pos[1], pos[2]);
        }

        public static void SetPosition(Vertex v, Vector3 pos)
        {
            float[] ch = v.Channels["position"];
            ch[0] = pos.X;
            ch[1] = pos.Y;
            ch[2] = pos.Z;
        }
    }
}