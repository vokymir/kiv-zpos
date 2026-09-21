using System;
using System.Collections.Generic;
using Core.IO;
using Core.Structures;
using Renderer.Components;
using Renderer.Wrappers.Model;
using System.Numerics;
using Visualizer;

class Program
{
    static void Main(string[] args)
    {
        // Setup: Load the triangle mesh
        IMeshImporter importer = MeshImporterFactory.Resolve("obj");
        MeshData data = importer.Load("Assets/Models/lion.obj");
        Vertex[] vertices = data.Vertices;
        int[] indices = data.Indices;

        // Task 1: Access vertex positions and triangle indices
        int faceIndex = 100;
        int[] idxs = { indices[3 * faceIndex], indices[3 * faceIndex + 1], indices[3 * faceIndex + 2] };
        float[][] vrts = { vertices[idxs[0]].Channels["position"], vertices[idxs[1]].Channels["position"], vertices[idxs[2]].Channels["position"] };
        Console.WriteLine($"Triangle {faceIndex}: \nv1: {idxs[0]}\nX={vrts[0][0]}\nY={vrts[0][1]}\nZ={vrts[0][2]} \nv2: {idxs[1]}\nX={vrts[1][0]}\nY={vrts[1][1]}\nZ={vrts[1][2]} \nv1: {idxs[2]}\nX={vrts[2][0]}\nY={vrts[2][1]}\nZ={vrts[2][2]}");

        // Write your own code here! 
        // Find the indices for face 100, extract the vertex coordinates, and print them.


        // Task 2: Assign colors to vertices

        // Write your own code here! 
        // Think of some fun way to compute colors for the mesh vertices,
        // then store them in vertex.Channels["color"].

        float X_MIN = float.PositiveInfinity;
        float X_MAX = float.NegativeInfinity;
        float Y_MIN = float.PositiveInfinity;
        float Y_MAX = float.NegativeInfinity;
        float Z_MIN = float.PositiveInfinity;
        float Z_MAX = float.NegativeInfinity;
        foreach (var vtx in vertices)
        {
            var x = vtx.Channels["position"][0];
            var y = vtx.Channels["position"][1];
            var z = vtx.Channels["position"][2];
            if (x > X_MAX)
            {
                X_MAX = x;
            }
            if (x < X_MIN)
            {
                X_MIN = x;
            }

            if (y > Y_MAX)
            {
                Y_MAX = y;
            }
            if (y < Y_MIN)
            {
                Y_MIN = y;
            }

            if (z > Z_MAX)
            {
                Z_MAX = z;
            }
            if (z < Z_MIN)
            {
                Z_MIN = z;
            }
        }

        float X_CONST = X_MAX / X_MIN;
        float Y_CONST = Y_MAX / Y_MIN;
        float Z_CONST = Z_MAX / Z_MIN;

        for (int i = 0; i < vertices.Length; i++)
        {
            var vtx = vertices[i];
            float r, g, b;
            //r = (float)i / vertices.Length;
            //g = vtx.Channels["position"][0] * X_CONST;
            //b = (float)Math.Sin(i);
            r = Math.Abs(vtx.Channels["position"][0] * X_CONST);
            g = Math.Abs(vtx.Channels["position"][1] * Y_CONST);
            b = Math.Abs(vtx.Channels["position"][2] * Z_CONST);

            vertices[i].Channels["color"] = [ r, g, b, 1.0f ];
        }


        // Task 3: Visualize per-vertex vectors

        // Write your own code here!
        // Generate some arbitrary per-vertex vectors and store them in vertex.Channels["normal"].
        // You can view them by checking 'Normals' in the visualizer UI toolbar
        // - even though they're probably not true normals yet!

        foreach (var vtx in vertices)
        {
            vtx.Channels["normal"] = [1.0f, 1.0f, 1.0f];
        }


        // Task 4 & 5: Computation of triangle and vertex normals

        // Write your own code here! 
        // Try to do this efficiently!
        // When your normals are ready, overwrite the values in vertex.Channels["normal"].

        Vector3[] facesNormals = new Vector3[indices.Length / 3];
        Vector3[] verticesNormals = new Vector3[vertices.Length];

        for (var i = 0; i < indices.Length - 2; i = i+3)
        {
            Vector3 v1 = vertices[indices[i]];
            Vector3 v2 = vertices[indices[i + 1]];
            Vector3 v3 = vertices[indices[i + 2]];

            Vector3 v2_v1 = v2 - v1;
            Vector3 v3_v1 = v3 - v1;

            var normal = Vector3.Normalize(Vector3.Cross(v2_v1, v3_v1));

            facesNormals[i / 3] = normal;

            verticesNormals[indices[i]] += normal;
            verticesNormals[indices[i+1]] += normal;
            verticesNormals[indices[i+2]] += normal;
        }

        for (var i = 0; i < verticesNormals.Length; i++)
        {
            verticesNormals[i] = Vector3.Normalize(verticesNormals[i]);
            var vn = verticesNormals[i];
            vertices[i].Channels["normal"] = [vn.X, vn.Y, vn.Z];
        }
        


        // Setup: Define GPU layout mapping
        List<VertexChannelSpec> layout =
        [
            new() { Name = "position", AttributeIndex = 0 },
            new() { Name = "normal",   AttributeIndex = 1 },
            new() { Name = "color",    AttributeIndex = 2 }
        ];

        // Setup: Launch visualizer session and upload data
        VisualizerSession.Run(session =>
        {
            session.SetMesh(vertices, indices, layout, null);
        });
    }
}