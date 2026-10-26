using Renderer.Abstractions;
using Renderer.Wrappers.Textures;

namespace Connector
{
	internal sealed class ImGuiTextureRegistry
	{
		private readonly Dictionary<nint, Texture2D> _textures = [];
		private nint _nextId = 1;

		public UiTextureHandle Register(Texture2D texture)
		{
			ArgumentNullException.ThrowIfNull(texture);

			nint id = _nextId++;
			_textures.Add(id, texture);
			return new UiTextureHandle(id);
		}

		public bool Unregister(UiTextureHandle handle)
		{
			return _textures.Remove(handle.Id);
		}

		public Texture2D Resolve(nint id)
		{
			if (!_textures.TryGetValue(id, out Texture2D? texture))
				throw new KeyNotFoundException($"No texture registered for ImGui texture ID {id}.");

			return texture;
		}
	}
}
