using OpenTK.Graphics.OpenGL;

namespace Renderer.Wrappers.Model
{
	public readonly struct VertexAttribute(int index, int componentCount, VertexAttribPointerType type, bool normalized, int strideBytes, int offsetBytes)
	{
		public int  Index          { get; } = index;
		public int  ComponentCount { get; } = componentCount;
		public VertexAttribPointerType Type { get; } = type;
		public bool Normalized     { get; } = normalized;
		public int  StrideBytes    { get; } = strideBytes;
		public int  OffsetBytes    { get; } = offsetBytes;
	}
}
