using Core.Structures;
using OpenTK.Graphics.OpenGL;

namespace Renderer.Wrappers.Model
{
	/// <summary>
	/// Maps a named vertex channel to a GPU attribute slot.
	/// The component count and byte offsets are computed at upload time from the actual channel data.
	/// </summary>
	public sealed class VertexChannelSpec
	{
		/// <summary>Channel identifier matching a key in <see cref="Core.Structures.Vertex.Channels"/>.</summary>
		public string Name           { get; set; } = "";
		/// <summary>GPU attribute index (location in the vertex shader).</summary>
		public int    AttributeIndex { get; set; }
		/// <summary>GL vertex attribute data type. Defaults to <see cref="VertexAttribPointerType.Float"/>.</summary>
		public VertexAttribPointerType GlType { get; set; } = VertexAttribPointerType.Float;
		public bool   Normalized     { get; set; }
		/// <summary>Per-spec rescaling applied across all vertices before upload. See <see cref="ChannelNormalization"/>.</summary>
		public ChannelNormalization Normalization { get; set; } = ChannelNormalization.None;
	}
}
