using System.Numerics;
using System.Runtime.CompilerServices;
using Core.IO;
using Core.Structures;
using Renderer.Wrappers.Model;
using Visualizer;

class Program
{
    static void Main(string[] args)
    {
        // 1. Initialize Mesh Data
        IMeshImporter importer = MeshImporterFactory.Resolve("obj");
        //MeshData data = importer.Load("Assets/Models/elliptical.obj");
        //MeshData data = importer.Load("Assets/Models/hyperbolic.obj");
        MeshData data = importer.Load("Assets/Models/parabolic.obj");
        Vertex[] vertices = data.Vertices;
        int[] indices = data.Indices;

        // 2. Build Topology
        CornerTable table = new CornerTable(indices, vertices.Length);

        // 3. Estimate Gaussian Curvature (g = 2pi - sum(alpha))
        // TODO: Compute the curvature 'g' for each vertex using the CornerTable.
        // Note the IsOnBorder method stub in the CornerTable class.
        float max_g = float.NegativeInfinity;
        float min_g = float.PositiveInfinity;
        for (int i = 0; i < vertices.Length; i++)
        {
            if (table.IsOnBorder(i)) {
                vertices[i].Channels["g"] = [0];
                max_g = MathF.Max(max_g, 0);
                min_g = MathF.Min(min_g, 0);
                continue;
            }
            var faces = table.VF(i);
            
            float angles = 0;

            for (int j = 0; j < faces.Length; j++) { 
                var inds = new int[] {indices[3 * faces[j] + 0], indices[3 * faces[j] + 1], indices[3 * faces[j] + 2]};

                int orig = 0;
                for (int k = 0; k < inds.Length; k++) {
                    if (inds[k] == i) { orig = k; break; }
                }

                // here v1 is always the middle vertex
                var v1 = vertices[inds[orig]];
                var v2 = vertices[inds[(orig + 1) % inds.Length]];
                var v3 = vertices[inds[(orig + 2) % inds.Length]];

                var v1_pos = GetPosition(v1);
                var v2_pos = GetPosition(v2);
                var v3_pos = GetPosition(v3);

                var vec1 = Vector3.Normalize(v2_pos - v1_pos);
                var vec2 = Vector3.Normalize(v3_pos - v1_pos);

                float dotProduct = Vector3.Dot(vec1, vec2);
                dotProduct = Math.Clamp(dotProduct, -1.0f, 1.0f);
                float angleInRadians = MathF.Acos(dotProduct);

                if (angleInRadians is not float.NaN && angleInRadians > 0)
                {
                    angles += angleInRadians;
                }
            }

            //var g = (float)(2 * Math.PI - (double)angles);
            var g = 2 * (float)Math.PI - angles;

            vertices[i].Channels["g"] = [g]; 
            max_g = MathF.Max(max_g, g);
            min_g = MathF.Min(min_g, g);
        }

        // 4. Map g to Color Channels
        // TODO: Transform the values of g into color.
        // Colors should be represented as a float array with values ranging from 0.0 to 1.0 (e.g., [r, g, b, a]).
        // Ideally, you should use the full scale of the colors, i.e. for example map the largest negative value
        // of g to full red color, zero g value to blue and largest positive g to green
        // Use a fallback color when curvature cannot be reliably calculated.

        foreach (var v in vertices) {
            Console.WriteLine(v.Channel("g")[0]);
            var gg = v.Channel("g")[0];
            var color = Get_Color(gg, min_g, max_g);
            v.Channels["color"] = color;
        }
        Console.WriteLine(min_g);
        Console.WriteLine(max_g);

        // 5. Expose Vertex Channels to the Framework
        List<VertexChannelSpec> layout =
        [
            new() { Name = "position", AttributeIndex = 0 },
            new() { Name = "normal",   AttributeIndex = 1 },
            new() { Name = "color",    AttributeIndex = 2 }
        ];

        // 6. Launch Application
        VisualizerSession.Run(session =>
        {
            session.SetMesh(vertices, indices, layout, null);
        });
    }

    private static float[] Get_Color(float gg, float gg_min, float gg_max)
    {
        if (float.IsNaN(gg))
        {
            return new float[4] { 0.5f, 0.5f, 0.5f, 1f }; 
        }

        float r = 0, g = 0, b = 1, a = 1; 

        const float EPSILON = 1e-6f;


        if (gg <= 0)
        {
            float divisor = Math.Abs(gg_min);
            if (divisor < EPSILON)
            {
                return new float[4] { r, g, b, a };
            }

            float factor = Math.Abs(gg) / divisor;
                factor = Math.Clamp(factor, 0f, 1f);

                r = factor;
                b = 1f - factor;
                g = 0f;
        }
        else
        {
            float divisor = Math.Abs(gg_max);
            if (divisor < EPSILON)
            {
                return new float[4] { r, g, b, a };
            }

            float factor = gg / divisor;
                factor = Math.Clamp(factor, 0f, 1f);

                g = factor;
                b = 1f - factor;
                r = 0f;
        }

        return new float[4] { r, g, b, a };
    }


    /// <summary>
    /// Helper method to extract a System.Numerics.Vector3 position from a Core.Structures.Vertex
    /// </summary>
    private static Vector3 GetPosition(Vertex v)
    {
        float[] pos = v.Channels["position"];
        return new Vector3(pos[0], pos[1], pos[2]);
    }
}