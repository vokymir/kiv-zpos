using OpenTK.Graphics.OpenGL;

namespace Renderer.Wrappers.Model
{
	public sealed class VertexLayout
	{
		private readonly List<VertexAttribute> _attributes = [];
		public IReadOnlyList<VertexAttribute> Attributes => _attributes;

		public VertexLayout Add(int index, int componentCount, VertexAttribPointerType type, bool normalized, int strideBytes, int offsetBytes)
		{
			_attributes.Add(new VertexAttribute(index, componentCount, type, normalized, strideBytes, offsetBytes));
			return this;
		}
	}
}
