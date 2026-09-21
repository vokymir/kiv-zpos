using ImGuiNET;
using Renderer.Objects;
using Renderer.Abstractions;
using Renderer.Wrappers.Textures;
using System.Numerics;
using Visualizer.Scripts;
using Visualizer.UI;

namespace Visualizer.GUI
{
	internal sealed class SceneViewportPanel
	{
		public Vector2 Size    { get; private set; }
		public bool Hovered    { get; private set; }
		public bool Focused    { get; private set; }

		private Vector2 _imageOrigin;

		public void Draw(
			UiTextureHandle texture,
			SelectionController selection,
			SelectionMode selectionMode,
			Vector2 mousePos,
			bool mouseRightClicked,
			Camera? camera,
			Entity? entity)
		{
			ImGuiViewportPtr viewport = ImGui.GetMainViewport();
			float topOffset  = ImGui.GetFrameHeight();
			Vector2 workPos  = viewport.WorkPos;
			Vector2 workSize = viewport.WorkSize;

			float leftWidth  = workSize.X * 0.20f;
			float rightWidth = workSize.X * 0.25f;

			ImGui.SetNextWindowPos(
				new Vector2(workPos.X + leftWidth, workPos.Y + topOffset),
				ImGuiCond.FirstUseEver);
			ImGui.SetNextWindowSize(
				new Vector2(workSize.X - leftWidth - rightWidth, workSize.Y - topOffset),
				ImGuiCond.FirstUseEver);

			ImGui.Begin("Scene");

			Focused = ImGui.IsWindowFocused();

			Vector2 avail = ImGui.GetContentRegionAvail();
			Size = new Vector2(MathF.Max(1f, avail.X), MathF.Max(1f, avail.Y));

			if (texture.Id != 0)
			{
				_imageOrigin = ImGui.GetCursorScreenPos();
				ImGui.Image(texture.Id, Size, new Vector2(0, 1), new Vector2(1, 0));
				Hovered = ImGui.IsItemHovered();

				if (mouseRightClicked && Hovered && camera is not null && entity is not null)
					selection.TryPick(selectionMode, mousePos, _imageOrigin, Size, camera, entity);

				if (camera is not null && entity is not null)
					selection.DrawOverlays(_imageOrigin, Size, camera, entity);

				if (camera is not null)
					CoordGizmo.Draw(ImGui.GetWindowDrawList(), _imageOrigin, Size, camera);
			}
			else
			{
				Hovered = false;
			}

			ImGui.End();
		}
	}
}
