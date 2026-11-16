using ImGuiNET;
using Renderer.Objects;
using System.Numerics;

namespace Visualizer.GUI
{
	internal sealed class HierarchyPanel
	{
		private SceneHierarchyItem? _root;
		private string? _selectedItemId;

		public object? SelectedObject { get; private set; }

		public Action? OnAddDirectionalLight    { get; set; }
		public Action<object>? OnRemoveDirectionalLight { get; set; }

		public void SetRoot(SceneHierarchyItem root) => _root = root;

		public void Draw()
		{
			ImGuiViewportPtr viewport = ImGui.GetMainViewport();
			float topOffset = ImGui.GetFrameHeight();
			Vector2 workPos  = viewport.WorkPos;
			Vector2 workSize = viewport.WorkSize;

			ImGui.SetNextWindowPos(
				new Vector2(workPos.X, workPos.Y + topOffset),
				ImGuiCond.FirstUseEver);
			ImGui.SetNextWindowSize(
				new Vector2(workSize.X * 0.20f, workSize.Y - topOffset),
				ImGuiCond.FirstUseEver);

			ImGui.Begin("Hierarchy");

			if (_root is null)
			{
				ImGui.TextDisabled("No scene loaded.");
				ImGui.End();
				return;
			}

			DrawNode(_root);
			ImGui.End();
		}

		private void DrawNode(SceneHierarchyItem item)
		{
			bool isLeaf = item.Children.Count == 0;

			ImGuiTreeNodeFlags flags =
				ImGuiTreeNodeFlags.OpenOnArrow |
				ImGuiTreeNodeFlags.OpenOnDoubleClick |
				ImGuiTreeNodeFlags.DefaultOpen;

			if (isLeaf)
				flags |= ImGuiTreeNodeFlags.Leaf;
			if (_selectedItemId == item.Id)
				flags |= ImGuiTreeNodeFlags.Selected;

			if (item.Type == SceneItemType.Entity)
			{
				bool wasVisible = item.Visible;
				string eyeIcon = wasVisible ? Icons.EyeOpen : Icons.EyeClosed;
				ImGui.PushStyleColor(ImGuiCol.Button,        Vector4.Zero);
				ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1f, 1f, 1f, 0.1f));
				ImGui.PushStyleColor(ImGuiCol.ButtonActive,  new Vector4(1f, 1f, 1f, 0.2f));
				if (!wasVisible)
					ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1f));
				if (ImGui.SmallButton($"{eyeIcon}##vis_{item.Id}"))
					item.Visible = !item.Visible;
				ImGui.PopStyleColor(!wasVisible ? 4 : 3);
				ImGui.SameLine();
			}

			bool dimmed = item.Type == SceneItemType.Entity && !item.Visible;
			if (dimmed)
				ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1f));
			bool open = ImGui.TreeNodeEx($"{GetLabel(item)}##{item.Id}", flags);
			if (dimmed)
				ImGui.PopStyleColor();

			if (ImGui.IsItemClicked())
			{
				_selectedItemId = item.Id;
				SelectedObject  = item.Target;
			}

			string popupId = $"ctx_{item.Id}";
			if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
				ImGui.OpenPopup(popupId);

			if (ImGui.BeginPopup(popupId))
			{
				if (item.Type == SceneItemType.Group && item.Id == "group-lights")
				{
					if (ImGui.MenuItem("Add Directional Light"))
						OnAddDirectionalLight?.Invoke();
				}
				else if (item.Type == SceneItemType.Light && item.Target is DirectionalLight)
				{
					if (ImGui.MenuItem("Delete"))
						OnRemoveDirectionalLight?.Invoke(item.Target);
				}
				ImGui.EndPopup();
			}

			if (open)
			{
				foreach (SceneHierarchyItem child in item.Children)
					DrawNode(child);
				ImGui.TreePop();
			}
		}

		private static string GetLabel(SceneHierarchyItem item) => item.Type switch
		{
			SceneItemType.Root   => $"Scene: {item.Name}",
			SceneItemType.Group  => $"Group: {item.Name}",
			_                    => item.Name
		};
	}
}
