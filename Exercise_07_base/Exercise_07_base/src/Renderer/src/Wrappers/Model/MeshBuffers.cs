using Core.Structures;
using OpenTK.Graphics.OpenGL;

namespace Renderer.Wrappers.Model
{
	public sealed class MeshBuffers : GLDisposable
	{
		public VertexArray VertexArray     { get; } = new();
		public VertexBuffer VertexBuffer   { get; } = new();
		public ElementBuffer ElementBuffer { get; } = new();

		public MeshBuffers()
		{
			VertexArray.SetVertexBuffer(VertexBuffer);
			VertexArray.SetElementBuffer(ElementBuffer);
		}

		public void SetVertices(int vertexCount, IReadOnlyList<float[][]> channels)
		{
			VertexArray.Bind();
			VertexBuffer.SetData(BufferUsageHint.StaticDraw, Vertex.ToFloats(vertexCount, channels));
		}

		public void SetIndices(int[] indices)
		{
			VertexArray.Bind();
			ElementBuffer.SetData(BufferUsageHint.StaticDraw, indices);
		}

		public void SetLayout(VertexLayout layout)
		{
			VertexArray.ApplyLayout(layout);
		}

		protected override void GLDispose()
		{
			ElementBuffer.Dispose();
			VertexBuffer.Dispose();
			VertexArray.Dispose();
		}
	}
}
