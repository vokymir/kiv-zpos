using Core.IO;
using Core.Structures;
using OpenTK.Graphics.OpenGL;
using Renderer.Wrappers.Model;

namespace Renderer.Components
{
	/// <summary>
	/// A renderable mesh: owns GPU buffers and exposes the CPU-side vertex/index arrays
	/// so other systems (e.g. ray picking) can read them without round-tripping to the GPU.
	/// </summary>
	public class Mesh : GLDisposable
	{
		public MeshBuffers Buffers { get; } = new();
		public MeshPrimitive   Primitive { get; set; } = MeshPrimitive.Triangles;
		public DrawElementsType IndexType { get; set; } = DrawElementsType.UnsignedInt;
		public int      IndexCount   { get; private set; }
		public Vertex[] Vertices     { get; private set; } = [];
		public int[]    Indices      { get; private set; } = [];
		public NormalsSource NormalsSource { get; private set; } = NormalsSource.Generated;

		/// <inheritdoc cref="SetGeometry(Vertex[], int[], List{VertexChannelSpec}?)"/>
		public void SetGeometry(MeshData data, List<VertexChannelSpec>? channelSpec = null)
		{
			NormalsSource = data.NormalsSource;
			SetGeometry(data.Vertices, data.Indices, channelSpec);
		}

		/// <summary>
		/// Uploads geometry to the GPU and stores CPU copies of <paramref name="vertices"/> and <paramref name="indices"/>.
		/// </summary>
		/// <param name="channelSpec">
		/// Ordered list that maps named vertex channels to GPU attribute slots.
		/// Component counts, stride, and offsets are computed from the actual channel sizes in the first vertex.
		/// When <see langword="null"/>, the default spec (position→0, normal→1) is used.
		/// </param>
		public void SetGeometry(Vertex[] vertices, int[] indices, List<VertexChannelSpec>? channelSpec = null)
		{
			channelSpec ??= DefaultChannelSpec;

			if (vertices.Length > 0)
			{
				// Resolve each spec's channel data independently (cached by name+mode) so the
				// same channel can be mapped both normalized and raw without one affecting the other.
				var cache = new Dictionary<(string Name, ChannelNormalization Mode), float[][]>();
				var channelData = new List<float[][]>(channelSpec.Count);

				foreach (VertexChannelSpec spec in channelSpec)
				{
					var key = (spec.Name, spec.Normalization);
					if (!cache.TryGetValue(key, out float[][]? resolved))
					{
						resolved = VertexNormalizer.Resolve(vertices, spec.Name, spec.Normalization);
						cache[key] = resolved;
					}
					channelData.Add(resolved);
				}

				int stride = channelData.Sum(d => d[0].Length) * sizeof(float);
				var layout = new VertexLayout();
				int offsetBytes = 0;

				for (int i = 0; i < channelSpec.Count; i++)
				{
					VertexChannelSpec spec = channelSpec[i];
					int count = channelData[i][0].Length;
					layout.Add(spec.AttributeIndex, count, spec.GlType, spec.Normalized, stride, offsetBytes);
					offsetBytes += count * sizeof(float);
				}

				Buffers.SetVertices(vertices.Length, channelData);
				Buffers.SetLayout(layout);

				// Keep CPU-side vertex data (used by picking/overlays) consistent with what's
				// rendered: replace normalized channels with their resolved copies (first spec wins).
				var appliedChannels = new HashSet<string>();
				foreach (VertexChannelSpec spec in channelSpec)
				{
					if (spec.Normalization == ChannelNormalization.None || !appliedChannels.Add(spec.Name))
						continue;

					float[][] resolved = cache[(spec.Name, spec.Normalization)];
					for (int i = 0; i < vertices.Length; i++)
						vertices[i].Channels[spec.Name] = resolved[i];
				}
			}

			Buffers.SetIndices(indices);
			IndexCount = indices.Length;
			Vertices   = vertices;
			Indices    = indices;
		}

		/// <summary>Binds the vertex array object for subsequent draw calls.</summary>
		public void Bind() => Buffers.VertexArray.Bind();

		/// <summary>Issues the indexed draw call for the currently bound VAO.</summary>
		public void Draw()
		{
			if (IndexCount <= 0)
				throw new InvalidOperationException("Mesh has no indices to draw.");

			GL.DrawElements((PrimitiveType)(int)Primitive, IndexCount, IndexType, 0);
		}

		protected override void GLDispose() => Buffers.Dispose();

		private static readonly List<VertexChannelSpec> DefaultChannelSpec =
		[
			new() { Name = "position", AttributeIndex = 0 },
			new() { Name = "normal",   AttributeIndex = 1 },
		];
	}
}
