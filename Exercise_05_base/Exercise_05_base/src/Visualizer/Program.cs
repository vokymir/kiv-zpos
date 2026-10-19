using Core.DataStructures;
using Core.IO;
using Core.PostProcessing;
using Core.Structures;
using Renderer.Wrappers.Model;
using System;
using System.Collections.Generic;
using System.Numerics;
using Visualizer;

class Program
{
    static void Main(string[] args)
    {
        // 1. Initialize Mesh Data
        IMeshImporter importer = MeshImporterFactory.Resolve("obj");
        //MeshData data = importer.Load("Assets/Models/sphere1.obj");
        //MeshData data = importer.Load("Assets/Models/lion.obj");
        MeshData data = importer.Load("Assets/Models/two_spheres.obj");

        Vertex[] vertices = data.Vertices;
        int[] indices = data.Indices;

        // Precompute surface normals for lighting and store them in "shading_normal".
        // This keeps the surface properly lit while we use the normals channel later to visualize the Laplacians.
        NormalsGenerator.Apply(vertices, indices, NormalsMode.Smooth);
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].Channels["shading_normal"] = (float[])vertices[i].Channels["normal"].Clone();
        }

        // 2. Build Topology
        CornerTable table = new CornerTable(indices, vertices.Length);

        // 3. Compute Laplacian Vectors
        //Vector3[] laplacians = ComputeTutteLaplacian(vertices, table);
        Vector3[] laplacians = ComputeCotanLaplacian(vertices, indices, table);

        // 4. Assign the computed vectors to the "normal" channel so they can be
        //    visualized in the viewport by pressing 'N' (or checking "Normals" in the toolbar).
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 v = laplacians[i];
            vertices[i].Channels["normal"] = [v.X, v.Y, v.Z];
        }

        // 5. Expose Vertex Channels to the GPU (uses "shading_normal" for lighting)
        List<VertexChannelSpec> layout =
        [
            new() { Name = "position",       AttributeIndex = 0 },
            new() { Name = "shading_normal", AttributeIndex = 1 }
        ];

        // 6. Launch Application
        VisualizerSession.Run(session =>
        {
            session.SetMesh(vertices, indices, layout, null);
        });
    }

    private static Vector3[] ComputeTutteLaplacian(Vertex[] vertices, CornerTable table)
    {
        Vector3[] result = new Vector3[vertices.Length];

        // TODO: Compute the combinatorial (Tutte) Laplacian for each vertex.
        for (int i = 0; i < vertices.Length; ++i)
        {
            var v = vertices[i];
            Vector3 sum = Vector3.Zero;
            var neighs = table.VV(i);

            foreach (var neigh in neighs)
            {
                sum += GetPosition(vertices[neigh]);
            }

            sum /= neighs.Length;

            sum -= GetPosition(v);

            result[i] = sum;
        }

        return result;
    }

    private static Vector3[] ComputeCotanLaplacian(Vertex[] vertices, int[] indices, CornerTable table)
    {
        Vector3[] result = new Vector3[vertices.Length];

        // TODO: Compute the cotan Laplacian for each vertex.
        for (int i = 0; i < vertices.Length; ++i) 
        {
            var neighs = table.VV(i);
            float[] alphas = new float[neighs.Length];
            float[] betas = new float[neighs.Length];
            float[] gamas = new float[neighs.Length];

            for (int j = 0; j < neighs.Length; ++j) {
                var angs = ComputeAngles(vertices[i], vertices[neighs[j]], vertices[neighs[(j+1)%neighs.Length]]);
                alphas[j] = angs[0];
                betas[j] = angs[1];
                gamas[j] = angs[2];
            }

            float[] omegas = new float[neighs.Length];
            for (int j = 0; j < neighs.Length; ++j) {
                var alpha = alphas[(j+alphas.Length - 1)%alphas.Length];
                var beta = betas[(j+0)%betas.Length];
                omegas[j] = 1f/2f * (1f/MathF.Tan(alpha) + 1f/MathF.Tan(beta));
            }

            float area = 0;
            for (int j = 0;j < neighs.Length; ++j)
            {
                var A = GetPosition(vertices[i]);
                var B = GetPosition(vertices[neighs[j]]);
                var C = GetPosition(vertices[neighs[(j + 1) % neighs.Length]]);

                var cj = Vector3.Zero;
                if (alphas[j] > Math.PI / 2 || betas[j] > Math.PI / 2 || gamas[j] > Math.PI / 2)
                { // middle of opposite side
                    cj = (GetPosition(vertices[neighs[j]]) + GetPosition(vertices[neighs[(j + neighs.Length - 1) % neighs.Length]])) / 2;
                }
                else {
                    var ab = B - A;
                    var ac = C - A;

                    var denom = 2.0 * Math.Pow(Vector3.Cross(ab,ac).Length(), 2.0);
                    var c_min_a_pow_2 = Math.Pow((ac).Length(), 2.0);
                    var big_cross = Vector3.Cross(Vector3.Cross(ab,ac),ab);
                    var b_min_a_pow_2 = Math.Pow((ab).Length(), 2.0);
                    var big_cross_2nd = Vector3.Cross(Vector3.Cross(ac, ab), ac);

                    var m = A + (((float)c_min_a_pow_2 * big_cross) + ((float)b_min_a_pow_2 * big_cross_2nd)) / (float)denom;

                    cj = m;
                }

                var center_AB = (A + B) / 2;
                var center_AC = (A + C) / 2;

                var area1 = ComputeArea((cj - A).Length(), (center_AB - A).Length(), (cj - center_AB).Length());
                var area2 = ComputeArea((cj - A).Length(), (center_AC - A).Length(), (cj - center_AC).Length());

                area += area1 + area2;
            }

            var sum = Vector3.Zero;
            var vi = GetPosition(vertices[i]);
            for (int j = 0; j < neighs.Length; ++j) {
                var vj = GetPosition(vertices[neighs[j]]);
                sum += omegas[j] * (vj - vi);
            }
            sum /= area;

            result[i] = sum;
        }

        return result;
    }

    private static float[] ComputeAngles(Vertex u_gamy, Vertex u_alfy, Vertex u_bety) {
        var v1_pos = GetPosition(u_gamy);
        var v2_pos = GetPosition(u_alfy);
        var v3_pos = GetPosition(u_bety);

        var vec1_2 = Vector3.Normalize(v2_pos - v1_pos);
        var vec1_3 = Vector3.Normalize(v3_pos - v1_pos);

        float dp_1 = Vector3.Dot(vec1_2, vec1_3);
        dp_1 = Math.Clamp(dp_1, -1.0f, 1.0f);
        float a_1 = MathF.Acos(dp_1);

        var vec2_1 = Vector3.Normalize(v1_pos - v2_pos);
        var vec2_3 = Vector3.Normalize(v3_pos - v2_pos);

        float dp_2 = Vector3.Dot(vec2_1, vec2_3);
        dp_2 = Math.Clamp(dp_2, -1.0f, 1.0f);
        float a_2 = MathF.Acos(dp_2);

        var vec3_1 = Vector3.Normalize(v1_pos - v3_pos);
        var vec3_2 = Vector3.Normalize(v2_pos - v3_pos);

        float dp_3 = Vector3.Dot(vec3_1, vec3_2);
        dp_3 = Math.Clamp(dp_3, -1.0f, 1.0f);
        float a_3 = MathF.Acos(dp_3);

        // return alfa,beta,gama
        return [a_2, a_3, a_1];
    }

    private static float ComputeArea(float a, float b, float c) {
        var s = (a + b + c) / 2; // semiperimeter
        return (float)Math.Sqrt(s*(s-a)*(s-b)*(s-c));
            }

    private static Vector3 GetPosition(Vertex v)
    {
        float[] pos = v.Channels["position"];
        return new Vector3(pos[0], pos[1], pos[2]);
    }
}