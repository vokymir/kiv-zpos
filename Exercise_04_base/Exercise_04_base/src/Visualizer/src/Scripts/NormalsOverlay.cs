using Core.Structures;
using Renderer.Components;
using Renderer.Objects;
using Renderer.Wrappers.Model;
using Renderer.Wrappers.Shaders;
using System.Numerics;

namespace Visualizer.Scripts
{
	/// <summary>Renders per-vertex normal arrows as GL_LINES on top of the scene.</summary>
	internal sealed class NormalsOverlay : IDisposable
	{
		// position→0, color→2 matches _normals.vert.glsl
		private static readonly List<VertexChannelSpec> ChannelSpec =
		[
			new() { Name = "position", AttributeIndex = 0 },
			new() { Name = "color",    AttributeIndex = 2 },
		];

		private Mesh? _linesMesh;
		private string? _builtFor;

		/// <summary>Forces the line mesh to rebuild on the next <see cref="Draw"/> call.</summary>
		public void InvalidateCache() => _builtFor = null;

		public void Draw(ShaderProgram shader, Entity entity, Camera camera, float aspectRatio, float scale)
		{
			if (entity.Mesh is null)
				return;

			string meshKey = $"{entity.Mesh.GetHashCode()}_{scale:F3}";
			if (meshKey != _builtFor)
				Rebuild(entity.Mesh, scale, meshKey);

			if (_linesMesh is null || _linesMesh.IndexCount == 0)
				return;

			Matrix4x4 model = entity.Transforms.Matrix;

			shader.Bind();
			shader.SetUniformImmediate("uModel", model);
			shader.SetUniformImmediate("uView", camera.GetViewMatrix());
			shader.SetUniformImmediate("uProjection", camera.GetProjectionMatrix(aspectRatio));

			_linesMesh!.Bind();
			_linesMesh.Draw();
		}

		private void Rebuild(Mesh source, float scale, string key)
		{
			Vertex[] src = source.Vertices;

			// Two vertices per normal: base and tip
			Vertex[] verts = new Vertex[src.Length * 2];
			int[]    indices = new int[src.Length * 2];

			// Root color (cyan) → tip color (yellow) for direction cue
			float[] baseColor = [0.2f, 0.9f, 1.0f, 1.0f];
			float[] tipColor  = [1.0f, 0.9f, 0.1f, 1.0f];

			for (int i = 0; i < src.Length; i++)
			{
				float[] pos  = src[i].Channels["position"];
				float[] norm = src[i].Channel("normal");
				float nx = norm.Length > 0 ? norm[0] : 0f;
				float ny = norm.Length > 1 ? norm[1] : 1f;
				float nz = norm.Length > 2 ? norm[2] : 0f;

				verts[i * 2] = new Vertex(new Dictionary<string, float[]>
				{
					["position"] = [pos[0], pos[1], pos[2]],
					["color"]    = [.. baseColor],
				});
				verts[i * 2 + 1] = new Vertex(new Dictionary<string, float[]>
				{
					["position"] = [pos[0] + nx * scale, pos[1] + ny * scale, pos[2] + nz * scale],
					["color"]    = [.. tipColor],
				});

				indices[i * 2]     = i * 2;
				indices[i * 2 + 1] = i * 2 + 1;
			}

			_linesMesh ??= new Mesh();
			_linesMesh.Primitive = MeshPrimitive.Lines;
			_linesMesh.SetGeometry(verts, indices, ChannelSpec);
			_builtFor = key;
		}

		public void Dispose() => _linesMesh?.Dispose();
	}
}
