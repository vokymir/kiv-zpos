using Core.Structures;
using Renderer.Components;
using Renderer.Objects;
using Renderer.Wrappers.Model;
using Renderer.Wrappers.Shaders;
using System.Numerics;

namespace Visualizer.Scripts
{
	internal sealed class FloorGrid : IDisposable
	{
		private static readonly List<VertexChannelSpec> ChannelSpec =
		[
			new() { Name = "position", AttributeIndex = 0 },
		];

		private readonly Mesh _mesh;

		public float FadeStart { get; set; } = 30f;
		public float FadeEnd   { get; set; } = 80f;

		public FloorGrid()
		{
			_mesh = new Mesh();

			// Three placeholder vertices; the vertex shader uses gl_VertexID for positions
			Vertex[] verts =
			[
				new(new Dictionary<string, float[]> { ["position"] = [0f, 0f, 0f] }),
				new(new Dictionary<string, float[]> { ["position"] = [0f, 0f, 0f] }),
				new(new Dictionary<string, float[]> { ["position"] = [0f, 0f, 0f] }),
			];
			_mesh.SetGeometry(verts, [0, 1, 2], ChannelSpec);
		}

		public void Draw(ShaderProgram shader, Camera camera, float aspectRatio)
		{
			Matrix4x4 view       = camera.GetViewMatrix();
			Matrix4x4 projection = camera.GetProjectionMatrix(aspectRatio);

			Matrix4x4.Invert(view,       out Matrix4x4 invView);
			Matrix4x4.Invert(projection, out Matrix4x4 invProjection);

			shader.Bind();
			shader.SetUniformImmediate("uInvView",        invView);
			shader.SetUniformImmediate("uInvProjection",  invProjection);
			shader.SetUniformImmediate("uViewProjection", view * projection);
			shader.SetUniformImmediate("uCameraPos",      camera.Transforms.Position);
			shader.SetUniformImmediate("uFadeStart",      FadeStart);
			shader.SetUniformImmediate("uFadeEnd",        FadeEnd);

			_mesh.Bind();
			_mesh.Draw();
		}

		public void Dispose() => _mesh.Dispose();
	}
}
