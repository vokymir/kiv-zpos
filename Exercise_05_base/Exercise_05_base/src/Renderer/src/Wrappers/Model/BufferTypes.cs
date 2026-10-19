using OpenTK.Graphics.OpenGL;

namespace Renderer.Wrappers.Model
{
	public sealed class VertexBuffer : BufferObject
	{
		public VertexBuffer() : base(BufferTarget.ArrayBuffer) {}
	}

	public sealed class ElementBuffer : BufferObject
	{
		public ElementBuffer() : base(BufferTarget.ElementArrayBuffer) {}
	}

	public sealed class ShaderStorageBuffer : BufferObject
	{
		public ShaderStorageBuffer() : base(BufferTarget.ShaderStorageBuffer) {}
	}
}
