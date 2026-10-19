using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Core.Structures;

namespace Core.IO
{
    /// <summary>Parses Wavefront OBJ files into GPU-ready vertex and index arrays.</summary>
    public class ObjImporter : IMeshImporter
    {
        public MeshData Load(string path)
        {
            using StreamReader reader = new(path);
            return Load(reader);
        }

        public MeshData Load(TextReader reader)
        {
            List<Vertex> vertices = [];
            List<int> indices = [];

            string? line;
            while ((line = reader.ReadLine()) is not null)
            {
                line = line.Trim();
                if (line.Length == 0 || line.StartsWith('#'))
                    continue;

                string[] parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                    continue;

                if (parts[0] == "v")
                {
                    // 1. Add vertices sequentially exactly as they appear in the file (matches old framework)
                    float x = float.Parse(parts[1], CultureInfo.InvariantCulture);
                    float y = float.Parse(parts[2], CultureInfo.InvariantCulture);
                    float z = float.Parse(parts[3], CultureInfo.InvariantCulture);
                    vertices.Add(Vertex.Standard(x, y, z));
                }
                else if (parts[0] == "f")
                {
                    // 2. Parse faces and directly map to the sequential vertices
                    List<int> faceIndices = [];
                    for (int i = 1; i < parts.Length; i++)
                    {
                        string[] refs = parts[i].Split('/');
                        int rawIndex = int.Parse(refs[0], CultureInfo.InvariantCulture);
                        int zeroBased = rawIndex > 0 ? rawIndex - 1 : vertices.Count + rawIndex;
                        faceIndices.Add(zeroBased);
                    }

                    // Fan triangulation for polygons
                    for (int i = 1; i < faceIndices.Count - 1; i++)
                    {
                        indices.Add(faceIndices[0]);
                        indices.Add(faceIndices[i]);
                        indices.Add(faceIndices[i + 1]);
                    }
                }
            }

            return new MeshData([.. vertices], [.. indices], NormalsSource.Generated);
        }
    }
}