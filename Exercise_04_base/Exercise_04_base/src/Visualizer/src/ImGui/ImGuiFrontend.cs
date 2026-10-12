using ImGuiNET;
using Renderer.Abstractions;
using Connector;

namespace Visualizer.UI
{
	public sealed class ImGuiFrontend : IDisposable
	{
		private readonly IImGuiBackend _backend;
		private bool _disposed;

		public ImGuiFrontend(IImGuiBackend backend, Action<ImGuiIOPtr>? fontSetup = null)
		{
			_backend = backend ?? throw new ArgumentNullException(nameof(backend));

			ImGui.CreateContext();
			ImGui.SetCurrentContext(ImGui.GetCurrentContext());

			ImGuiIOPtr io = ImGui.GetIO();
			io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
			io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

			ImGui.StyleColorsDark();
			OxocarbonTheme.Apply();

			fontSetup?.Invoke(io);

			_backend.Initialize();
		}

		public void BeginFrame(ImGuiFrameInput input)
		{
			ThrowIfDisposed();
			_backend.BeginFrame(input);
		}

		public void EndFrame()
		{
			ThrowIfDisposed();
			_backend.EndFrame();
		}

		public UiTextureHandle RegisterTexture(Renderer.Wrappers.Textures.Texture2D texture)
		{
			ThrowIfDisposed();
			return _backend.RegisterTexture(texture);
		}

		public bool UnregisterTexture(UiTextureHandle handle)
		{
			ThrowIfDisposed();
			return _backend.UnregisterTexture(handle);
		}

		public void Dispose()
		{
			if (_disposed)
				return;

			_backend.Dispose();

			IntPtr ctx = ImGui.GetCurrentContext();
			if (ctx != IntPtr.Zero)
				ImGui.DestroyContext(ctx);

			_disposed = true;
			GC.SuppressFinalize(this);
		}

		private void ThrowIfDisposed()
		{
			ObjectDisposedException.ThrowIf(_disposed, this);
		}
	}
}
