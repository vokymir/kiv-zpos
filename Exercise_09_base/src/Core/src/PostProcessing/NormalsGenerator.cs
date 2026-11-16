using Core.Structures;
using System.Numerics;

namespace Core.PostProcessing
{
	public static class NormalsGenerator
	{
		public static void Apply(Vertex[] vertices, int[] indices, NormalsMode mode)
		{
			switch (mode)
			{
				case NormalsMode.Flat:   ApplyFlat(vertices, indices);   break;
				case NormalsMode.Smooth: ApplySmooth(vertices, indices); break;
			}
		}

		private static void ApplyFlat(Vertex[] vertices, int[] indices)
		{
			for (int f = 0; f < indices.Length / 3; f++)
			{
				int i0 = indices[f * 3], i1 = indices[f * 3 + 1], i2 = indices[f * 3 + 2];

				float[] a = vertices[i0].Channels["position"];
				float[] b = vertices[i1].Channels["position"];
				float[] c = vertices[i2].Channels["position"];

				var p0 = new Vector3(a[0], a[1], a[2]);
				var p1 = new Vector3(b[0], b[1], b[2]);
				var p2 = new Vector3(c[0], c[1], c[2]);

				Vector3 cross = Vector3.Cross(p1 - p0, p2 - p0);
				Vector3 n = cross.LengthSquared() > 1e-12f ? Vector3.Normalize(cross) : Vector3.UnitY;

				WriteNormal(vertices[i0], n);
				WriteNormal(vertices[i1], n);
				WriteNormal(vertices[i2], n);
			}
		}

		private static void ApplySmooth(Vertex[] vertices, int[] indices)
		{
			// Step 1: accumulate face normals per vertex index.
			// Each parallel partition writes to its own array; merge under a lock at the end.
			var accumulated = new Vector3[vertices.Length];

			Parallel.For(
				0, indices.Length / 3,
				() => new Vector3[vertices.Length],
				(f, _, local) =>
				{
					int i0 = indices[f * 3], i1 = indices[f * 3 + 1], i2 = indices[f * 3 + 2];
					float[] a = vertices[i0].Channels["position"];
					float[] b = vertices[i1].Channels["position"];
					float[] c = vertices[i2].Channels["position"];
					var p0 = new Vector3(a[0], a[1], a[2]);
					var p1 = new Vector3(b[0], b[1], b[2]);
					var p2 = new Vector3(c[0], c[1], c[2]);
					var n  = Vector3.Normalize(Vector3.Cross(p1 - p0, p2 - p0));
					local[i0] += n;
					local[i1] += n;
					local[i2] += n;
					return local;
				},
				local =>
				{
					lock (accumulated)
						for (int i = 0; i < accumulated.Length; i++)
							accumulated[i] += local[i];
				});

			// Step 2: group accumulated normals by world position so UV-seam duplicates
			// share the same smooth normal (sequential — dictionary is not thread-safe).
			var positionNormals = new Dictionary<(float, float, float), Vector3>(vertices.Length);
			for (int i = 0; i < vertices.Length; i++)
			{
				float[] p = vertices[i].Channels["position"];
				var key = (p[0], p[1], p[2]);
				positionNormals[key] = positionNormals.TryGetValue(key, out var acc)
					? acc + accumulated[i]
					: accumulated[i];
			}

			// Step 3: normalize and write back in parallel (positionNormals is read-only here).
			Parallel.For(0, vertices.Length, i =>
			{
				float[] p = vertices[i].Channels["position"];
				var key = (p[0], p[1], p[2]);
				WriteNormal(vertices[i], Vector3.Normalize(positionNormals[key]));
			});
		}

		private static void WriteNormal(Vertex vertex, Vector3 n)
		{
			float[] norm = vertex.Channels["normal"];
			norm[0] = n.X;
			norm[1] = n.Y;
			norm[2] = n.Z;
		}
	}
}
