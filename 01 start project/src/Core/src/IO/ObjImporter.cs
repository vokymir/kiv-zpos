using System.Globalization;
using System.Numerics;
using Core.Structures;

namespace Core.IO
{
	/// <summary>Parses Wavefront OBJ files into GPU-ready vertex and index arrays.</summary>
	public class ObjImporter : IMeshImporter
	{
		private readonly record struct ObjVertexKey(int PositionIndex, int NormalIndex);

		/// <summary>Loads an OBJ file from <paramref name="path"/> and returns its mesh data.</summary>
		public MeshData Load(string path)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(path);

			using StreamReader reader = new(path);
			return Load(reader);
		}

		/// <summary>Parses OBJ data from <paramref name="reader"/> and returns its mesh data.</summary>
		public MeshData Load(TextReader reader)
		{
			ArgumentNullException.ThrowIfNull(reader);

			List<Vector3> positions = [];
			List<Vector3> normals = [];

			List<Vertex> vertices = [];
			List<int> indices = [];
			Dictionary<ObjVertexKey, int> vertexMap = [];

			string? line;
			int lineNumber = 0;

			while ((line = reader.ReadLine()) is not null)
			{
				lineNumber++;

				line = line.Trim();

				if (line.Length == 0 || line.StartsWith('#'))
					continue;

				string[] parts = SplitWhitespace(line);
				if (parts.Length == 0)
					continue;

				switch (parts[0])
				{
					case "v":
						ParsePosition(parts, positions, lineNumber);
						break;

					case "vn":
						ParseNormal(parts, normals, lineNumber);
						break;

					case "f":
						if (normals.Count > 0)
						{
							ParseFaceWithNormals(parts, positions, normals, vertices, indices, vertexMap, lineNumber);
						}
						else
						{
							ParseFaceWithoutNormals(parts, positions, vertices, indices, lineNumber);
						}
						break;

					case "vt":
					case "o":
					case "g":
					case "s":
					case "usemtl":
					case "mtllib":
						break;

					default:
						break;
				}
			}

			if (vertices.Count == 0 || indices.Count == 0)
				throw new InvalidDataException("OBJ did not produce any renderable geometry.");

			NormalsSource source = normals.Count > 0 ? NormalsSource.Loaded : NormalsSource.Generated;
			return new MeshData([.. vertices], [.. indices], source);
		}

		private static void ParsePosition(string[] parts, List<Vector3> positions, int lineNumber)
		{
			if (parts.Length < 4)
				throw new InvalidDataException($"Line {lineNumber}: vertex position requires 3 components.");

			positions.Add(new Vector3(
						ParseFloat(parts[1], lineNumber),
						ParseFloat(parts[2], lineNumber),
						ParseFloat(parts[3], lineNumber)));
		}

		private static void ParseNormal(string[] parts, List<Vector3> normals, int lineNumber)
		{
			if (parts.Length < 4)
				throw new InvalidDataException($"Line {lineNumber}: vertex normal requires 3 components.");

			Vector3 n = new(
					ParseFloat(parts[1], lineNumber),
					ParseFloat(parts[2], lineNumber),
					ParseFloat(parts[3], lineNumber));

			normals.Add(n.LengthSquared() > 0f ? Vector3.Normalize(n) : Vector3.UnitY);
		}

		private static void ParseFaceWithNormals(
				string[] parts,
				List<Vector3> positions,
				List<Vector3> normals,
				List<Vertex> vertices,
				List<int> indices,
				Dictionary<ObjVertexKey, int> vertexMap,
				int lineNumber)
		{
			if (parts.Length < 4)
				throw new InvalidDataException($"Line {lineNumber}: face must have at least 3 vertices.");

			List<int> faceIndices = [];

			for (int i = 1; i < parts.Length; i++)
			{
				int index = ResolveFaceVertex(
						parts[i], positions, normals, vertices, vertexMap, lineNumber);

				faceIndices.Add(index);
			}

			// Fan triangulation: (0,1,2), (0,2,3), ...
			for (int i = 1; i < faceIndices.Count - 1; i++)
			{
				indices.Add(faceIndices[0]);
				indices.Add(faceIndices[i]);
				indices.Add(faceIndices[i + 1]);
			}
		}

		private static int ResolveFaceVertex(
				string token,
				List<Vector3> positions,
				List<Vector3> normals,
				List<Vertex> vertices,
				Dictionary<ObjVertexKey, int> vertexMap,
				int lineNumber)
		{
			// Supported:
			// v
			// v//vn
			// v/vt/vn
			// v/vt
			string[] refs = token.Split('/');

			if (refs.Length is < 1 or > 3)
				throw new InvalidDataException($"Line {lineNumber}: invalid face token '{token}'.");

			int positionIndex = ParseObjIndex(refs[0], positions.Count, lineNumber);

			int normalIndex = -1;
			if (refs.Length == 3 && !string.IsNullOrWhiteSpace(refs[2]))
				normalIndex = ParseObjIndex(refs[2], normals.Count, lineNumber);

			ObjVertexKey key = new(positionIndex, normalIndex);

			if (vertexMap.TryGetValue(key, out int existingIndex))
				return existingIndex;

			Vector3 p = positions[positionIndex];
			Vector3 n = normalIndex >= 0 ? normals[normalIndex] : Vector3.UnitY;

			Vertex vertex = Vertex.Standard(p.X, p.Y, p.Z, n.X, n.Y, n.Z);

			int newIndex = vertices.Count;
			vertices.Add(vertex);
			vertexMap.Add(key, newIndex);
			return newIndex;
		}

		private static int ParseObjIndex(string token, int count, int lineNumber)
		{
			if (!int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out int rawIndex))
				throw new InvalidDataException($"Line {lineNumber}: invalid OBJ index '{token}'.");

			int zeroBased = rawIndex > 0
				? rawIndex - 1
				: count + rawIndex;

			if (zeroBased < 0 || zeroBased >= count)
				throw new InvalidDataException($"Line {lineNumber}: OBJ index '{rawIndex}' is out of range.");

			return zeroBased;
		}

		private static float ParseFloat(string token, int lineNumber)
		{
			if (!float.TryParse(token, NumberStyles.Float | NumberStyles.AllowThousands,
						CultureInfo.InvariantCulture, out float value))
			{
				throw new InvalidDataException($"Line {lineNumber}: invalid float '{token}'.");
			}

			return value;
		}

		private static string[] SplitWhitespace(string line) =>
			line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

		private static void ParseFaceWithoutNormals(
				string[] parts,
				List<Vector3> positions,
				List<Vertex> vertices,
				List<int> indices,
				int lineNumber)
		{
			if (parts.Length < 4)
				throw new InvalidDataException($"Line {lineNumber}: face must have at least 3 vertices.");

			List<int> facePositionIndices = [];

			for (int i = 1; i < parts.Length; i++)
			{
				string[] refs = parts[i].Split('/');

				if (refs.Length is < 1 or > 3)
					throw new InvalidDataException($"Line {lineNumber}: invalid face token '{parts[i]}'.");

				int positionIndex = ParseObjIndex(refs[0], positions.Count, lineNumber);
				facePositionIndices.Add(positionIndex);
			}

			for (int i = 1; i < facePositionIndices.Count - 1; i++)
			{
				Vector3 a = positions[facePositionIndices[0]];
				Vector3 b = positions[facePositionIndices[i]];
				Vector3 c = positions[facePositionIndices[i + 1]];

				int baseIndex = vertices.Count;

				vertices.Add(Vertex.Standard(a.X, a.Y, a.Z));
				vertices.Add(Vertex.Standard(b.X, b.Y, b.Z));
				vertices.Add(Vertex.Standard(c.X, c.Y, c.Z));

				indices.Add(baseIndex + 0);
				indices.Add(baseIndex + 1);
				indices.Add(baseIndex + 2);
			}
		}

	}
}
