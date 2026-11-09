namespace Visualizer.Controllers
{
	/// <summary>
	/// Shared mutable identity/state for the currently active scene, passed between
	/// <see cref="AppWindow"/> and its controllers so they agree on which asset is
	/// active and whether it has unsaved changes.
	/// </summary>
	public sealed class SceneState
	{
		public string CurrentModel { get; set; } = "";
		public string? CurrentAnimation { get; set; }
		public bool Modified { get; set; }
	}
}
