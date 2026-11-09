using ImGuiNET;

namespace Visualizer.Modals
{
	public enum UnsavedChangesResult { None, Save, Discard, Cancel }

	/// <summary>
	/// Modal dialog shown when the user tries to exit or switch models with unsaved scene changes.
	/// </summary>
	internal sealed class UnsavedChangesModal
	{
		private const string PopupId = "Unsaved Changes###UnsavedChangesModal";
		private bool _pendingOpen;

		public UnsavedChangesResult ResultThisFrame { get; private set; }

		public void Open() => _pendingOpen = true;

		public void Draw()
		{
			ResultThisFrame = UnsavedChangesResult.None;

			if (_pendingOpen)
			{
				ImGui.OpenPopup(PopupId);
				_pendingOpen = false;
			}

			ImGui.SetNextWindowSize(new System.Numerics.Vector2(380, 0), ImGuiCond.Always);
			if (!ImGui.BeginPopupModal(PopupId, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove))
				return;

			ImGui.Spacing();
			ImGui.TextWrapped("You have unsaved scene changes. What would you like to do?");
			ImGui.Spacing();
			ImGui.Separator();
			ImGui.Spacing();

			if (ImGui.Button("Save", new System.Numerics.Vector2(100, 0)))
			{
				ResultThisFrame = UnsavedChangesResult.Save;
				ImGui.CloseCurrentPopup();
			}
			ImGui.SameLine();
			if (ImGui.Button("Don't Save", new System.Numerics.Vector2(100, 0)))
			{
				ResultThisFrame = UnsavedChangesResult.Discard;
				ImGui.CloseCurrentPopup();
			}
			ImGui.SameLine();
			if (ImGui.Button("Cancel", new System.Numerics.Vector2(100, 0)))
			{
				ResultThisFrame = UnsavedChangesResult.Cancel;
				ImGui.CloseCurrentPopup();
			}

			ImGui.Spacing();
			ImGui.EndPopup();
		}
	}
}
