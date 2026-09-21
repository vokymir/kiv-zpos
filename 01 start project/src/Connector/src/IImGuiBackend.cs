using Renderer.Abstractions;
using Renderer.Wrappers.Textures;

namespace Connector
{
	public interface IImGuiBackend : IDisposable
	{
		void Initialize();
		void BeginFrame(ImGuiFrameInput input);
		void EndFrame();

		UiTextureHandle RegisterTexture(Texture2D texture);
		bool UnregisterTexture(UiTextureHandle handle);
	}
}
