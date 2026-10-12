using ImGuiNET;
using System.Numerics;

namespace Visualizer.GUI
{
	/// <summary>Thin bar docked to the bottom of the main viewport showing the loading
	/// spinner and transient status messages (e.g. "Scene Saved", "Model Reloaded").</summary>
	internal sealed class StatusBarPanel
	{
		private string? _message;
		private float   _messageExpiry;
		private float   _totalTime;
		private const float MessageDuration = 3f;

		public Func<bool>? IsLoadingProvider { get; set; }

		public void ShowMessage(string message)
		{
			_message = message;
			_messageExpiry = _totalTime + MessageDuration;
		}

		public void Draw(float deltaTime)
		{
			_totalTime += deltaTime;

			ImGuiViewportPtr viewport = ImGui.GetMainViewport();
			float height = ImGui.GetFrameHeight();

			// Reserve this strip at the bottom of the work area so the dockspace doesn't
			// place panels underneath it (mirrors how the main menu bar reserves space at top).
			viewport.WorkSize = new Vector2(viewport.WorkSize.X, viewport.WorkSize.Y - height);

			ImGui.SetNextWindowPos(new Vector2(viewport.Pos.X, viewport.Pos.Y + viewport.Size.Y - height));
			ImGui.SetNextWindowSize(new Vector2(viewport.Size.X, height));

			const ImGuiWindowFlags flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove
				| ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoDocking
				| ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav
				| ImGuiWindowFlags.NoBringToFrontOnFocus;

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(8f, ImGui.GetStyle().FramePadding.Y));
			ImGui.PushStyleColor(ImGuiCol.WindowBg, ImGui.GetStyle().Colors[(int)ImGuiCol.MenuBarBg]);
			ImGui.Begin("##StatusBar", flags);
			ImGui.PopStyleColor();
			ImGui.PopStyleVar();

			bool loading = IsLoadingProvider?.Invoke() == true;
			if (loading)
			{
				string spinner = Icons.ProgressSpinner[(int)(_totalTime * 10) % Icons.ProgressSpinner.Length];
				ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetStyle().Colors[(int)ImGuiCol.TextDisabled]);
				ImGui.TextUnformatted($"{spinner} Loading...");
				ImGui.PopStyleColor();
			}

			if (_message is not null && _totalTime < _messageExpiry)
			{
				if (loading)
					ImGui.SameLine();

				float remaining = _messageExpiry - _totalTime;
				float alpha = Math.Min(1f, remaining);
				ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.4f, 0.9f, 0.4f, alpha));
				ImGui.TextUnformatted(_message);
				ImGui.PopStyleColor();
			}

			ImGui.End();
		}
	}
}
