using System;
using System.Collections.Generic;
using Core.IO;
using Core.Structures;
using Renderer.Wrappers.Model;
using System.Numerics;
using Visualizer;

class Program
{
    static void Main(string[] args)
    {
        // Setup: Load the triangle mesh
        IMeshImporter importer = MeshImporterFactory.Resolve("obj");
        MeshData data = importer.Load("Assets/Models/lion2.obj"); //("Assets/Models/lion2.obj")
        Vertex[] vertices = data.Vertices;
        int[] indices = data.Indices;

        // ==============================================================================
        // Task 1: Initialization of the edge based data structure (3 points)
        // ==============================================================================
        // Choose and implement a data structure (Winged edge, Halfedge, Directed edge, or Corner table).
        // Ensure you achieve linear complexity using a Dictionary for edge lookups.

        // TODO: Instantiate your data structure here
        TriangleMesh tri = new TriangleMesh(vertices.Length);
        tri.initDataStructure(vertices, indices);

            Console.WriteLine("VV");
            var incVV = tri.VVincidenceQueryWithBounds(903);
            foreach (int vertexIDX in incVV!)
            {
                Console.WriteLine(vertexIDX);
            }


        


        // ==============================================================================
        // Task 2: Evaluation of the VF incidence query (2 points)
        // ==============================================================================
        // Implement a function in your data structure that accepts a vertex index
        // and returns an array or list of indices of all incident triangles (faces).
        // 
        // Optional extension (2 points): Adjust the implementation so that meshes 
        // with borders are handled as well.

        Console.WriteLine("VF");
        var incVF = tri.VFincidenceQueryWithBounds(903);
        foreach(int faceIDX in incVF!)
        {
            Console.WriteLine(faceIDX);
        }

        // Verification Prints (Uncomment to test your implementation once built)
        // Console.WriteLine($"o(0)={table.opposite[0]}");
        // Console.WriteLine($"o(1)={table.opposite[1]}");
        // Console.WriteLine($"o(2)={table.opposite[2]}");
        // Console.WriteLine($"o(3)={table.opposite[3]}");
        // Console.WriteLine($"o(4)={table.opposite[4]}");
        // 
        // Console.WriteLine($"\nVV(903):");
        // foreach (var v in table.VV(903)) Console.WriteLine(v);
        // 
        // Console.WriteLine($"\nVF(903):");
        // foreach (var f in table.VF(903)) Console.WriteLine(f);


        // ==============================================================================
        // Task 3: Computation of a vertex normal
        // ==============================================================================
        // Use your VF query to find all triangles incident to each vertex.
        // Calculate the normal of each incident triangle, accumulate them, and normalize.
        // Overwrite the normal channel: vertices[i].Channels["normal"] = [x, y, z];
        //var facesNormals = new Vector3[indices.Length/3]; // faceID to its normal
        for (int i = 0; i < vertices.Length; i++)
        {
            // TODO: Calculate normals here
            var faces = tri.VFincidenceQueryWithBounds(i);

            var normal = new Vector3();

            for (int j = 0; j < faces.Length; j++)
            {
                var n = getFaceNormal(i, vertices, indices);

                normal += n;
            }

            normal = Vector3.Normalize(normal);

            vertices[i].Channels["normal"] = [normal.X, normal.Y, normal.Z];
        }


        // Setup: Define GPU layout mapping
        List<VertexChannelSpec> layout =
        [
            new() { Name = "position", AttributeIndex = 0 },
            new() { Name = "normal",   AttributeIndex = 1 }
        ];

        // Setup: Launch visualizer session and upload data
        VisualizerSession.Run(session =>
        {
            session.SetMesh(vertices, indices, layout, null);
        });
    }

    static Vector3 getFaceNormal(int i, Vertex[] vertices, int[] indices)
    {
        Vector3 v1 = vertices[indices[i]];
        Vector3 v2 = vertices[indices[i + 1]];
        Vector3 v3 = vertices[indices[i + 2]];

        Vector3 v2_v1 = v2 - v1;
        Vector3 v3_v1 = v3 - v1;

        var normal = Vector3.Normalize(Vector3.Cross(v2_v1, v3_v1));

        return normal;
    }
}
