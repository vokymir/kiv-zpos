using Core.PostProcessing;
using ImGuiNET;
using Renderer.Abstractions;
using System.Numerics;
using Visualizer.Modals;

namespace Visualizer.GUI
{
	internal sealed class ToolbarPanel
	{
		/* ---- View state ---- */
		public ViewRenderMode RenderMode  { get; set; } = ViewRenderMode.Solid;
		public SelectionMode  SelectionMode { get; set; } = SelectionMode.Vertex;
		public bool BackfaceCulling { get; set; } = true;
		public bool ShowNormals  { get; set; } = false;
		public float NormalsScale { get; set; } = 1.0f;
		public bool ShowGrid    { get; set; } = true;

		/* ---- Model / shader state ---- */
		public string? SelectedModel  { get; set; }
		public string? CustomModelPath { get; set; }
		public bool ApiDataMode { get; set; }
		public string? SelectedShader { get; set; } = "default";

		/* ---- External state ---- */
		public bool IsSceneModified { get; set; }
		public Func<(AssetMetadata meta, string assetPath, string[] channels)?>? LayoutDataProvider { get; set; }

		/* ---- Per-frame output flags ---- */
		public bool ModelChangedThisFrame   { get; private set; }
		public bool ReloadModelRequested    { get; private set; }
		public bool ShaderChangedThisFrame  { get; private set; }
		public bool ReloadShaderRequested   { get; private set; }
		public bool LayoutChangedThisFrame  { get; private set; }
		public bool RequestResetCamera      { get; internal set; }
		public bool RequestFocusCamera      { get; internal set; }
		public bool SaveSceneRequested      { get; private set; }
		public bool ReloadSceneRequested    { get; private set; }
		public bool ResetSceneRequested     { get; private set; }

		/* ---- Modals ---- */
		private enum FilePickerMode { Model, Shader }
		private FilePickerMode _filePickerMode;
		private readonly FilePickerModal    _filePickerModal = new();
		private readonly VertexLayoutModal  _layoutModal     = new();

		public void ResetFrameFlags()
		{
			ModelChangedThisFrame  = false;
			ReloadModelRequested   = false;
			ShaderChangedThisFrame = false;
			ReloadShaderRequested  = false;
			LayoutChangedThisFrame = false;
			RequestResetCamera     = false;
			RequestFocusCamera     = false;
			SaveSceneRequested     = false;
			ReloadSceneRequested   = false;
			ResetSceneRequested    = false;
		}

		public void Draw(float deltaTime)
		{
			DrawToolbar();

			_layoutModal.Draw();
			if (_layoutModal.SavedThisFrame)
				LayoutChangedThisFrame = true;

			_filePickerModal.Draw();
			if (_filePickerModal.PickedThisFrame && _filePickerModal.PickedPath is not null)
			{
				if (_filePickerMode == FilePickerMode.Shader)
					ImportShader(_filePickerModal.PickedPath);
				else
					ImportModel(_filePickerModal.PickedPath);
			}
		}

		private void DrawToolbar()
		{
			var originalPadding = ImGui.GetStyle().FramePadding;
			ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(originalPadding.X, 10f));
			if (ImGui.BeginMainMenuBar())
			{
				ImGui.PopStyleVar();
				ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, originalPadding);

				float verticalOffset = 10f - originalPadding.Y;
				ImGui.SetCursorPosY(ImGui.GetCursorPosY() + verticalOffset);

				ImGui.SetNextItemWidth(200f);
				DrawModelSelector();
				ImGui.SameLine(0, 0);
				DrawImportButton();
				ImGui.SameLine(0, 0);
				DrawReloadModelButton();

				ToolbarSeparator();

				DrawLayoutButton();

				ToolbarSeparator();

				ImGui.SetNextItemWidth(130f);
				DrawShaderSelector();
				ImGui.SameLine(0, 0);
				DrawImportShaderButton();
				ImGui.SameLine(0, 0);
				DrawReloadShaderButton();

				ToolbarSeparator();

				DrawRenderModeSelector();

				ToolbarSeparator();

				DrawSelectionModeSelector();

				ToolbarSeparator();

				if (ImGui.Button("Reset Camera"))
					RequestResetCamera = true;
				ImGui.SameLine();
				if (ImGui.Button("Focus"))
					RequestFocusCamera = true;

				ToolbarSeparator();

				bool showNormals = ShowNormals;
				if (ImGui.Checkbox("Normals", ref showNormals))
					ShowNormals = !ShowNormals;

				if (ShowNormals)
				{
					ImGui.SameLine();
					ImGui.SetNextItemWidth(100f);
					float normalsScale = NormalsScale;
					if (ImGui.SliderFloat("Scale##normals", ref normalsScale, 0.01f, 10.0f))
						NormalsScale = normalsScale;
				}

				ToolbarSeparator();

				bool backfaceCulling = BackfaceCulling;
				if (ImGui.Checkbox("Backface Culling", ref backfaceCulling))
					BackfaceCulling = !BackfaceCulling;

				ToolbarSeparator();

				bool showGrid = ShowGrid;
				if (ImGui.Checkbox("Grid", ref showGrid))
					ShowGrid = !ShowGrid;

				ToolbarSeparator();

				if (IsSceneModified)
				{
					ToolbarText(Icons.Modified, new Vector4(1f, 0.75f, 0.3f, 1f));
					if (ImGui.IsItemHovered())
						ImGui.SetTooltip("Unsaved changes");
					ImGui.SameLine();
				}

				if (ImGui.Button($"{Icons.Save} Save Scene"))
					SaveSceneRequested = true;
				ImGui.SameLine();
				if (ImGui.Button($"{Icons.Reset} Reload Scene"))
					ReloadSceneRequested = true;
				ImGui.SameLine();
				if (ImGui.Button("Reset Scene"))
					ResetSceneRequested = true;

				float buttonW = ImGui.GetFrameHeight() + ImGui.GetStyle().FramePadding.X * 2f;
				ImGui.SetCursorPosX(ImGui.GetWindowWidth() - buttonW - ImGui.GetStyle().ItemSpacing.X);
				if (ImGui.Button($"{Icons.QuestionCircle}##keybinds"))
					ImGui.OpenPopup("##keybinds_popup");

				DrawKeybindsPopup();

				ImGui.PopStyleVar();
				ImGui.EndMainMenuBar();
			}
			else
			{
				ImGui.PopStyleVar();
			}
		}

		private void DrawRenderModeSelector()
		{
			DrawRenderModeButton($"{Icons.Cube}##Solid",      ViewRenderMode.Solid,      "Solid");
			ImGui.SameLine();
			DrawRenderModeButton($"{Icons.Grid}##Wireframe",  ViewRenderMode.Wireframe,  "Wireframe");
			ImGui.SameLine();
			DrawRenderModeButton($"{Icons.Circle}##Points",   ViewRenderMode.Points,     "Points");
		}

		private void DrawRenderModeButton(string label, ViewRenderMode mode, string tooltip) =>
			DrawToggleButton(label, RenderMode == mode, () => RenderMode = mode, tooltip);

		private void DrawSelectionModeSelector()
		{
			DrawSelectionModeButton($"{Icons.Vertex}##Vertices", SelectionMode.Vertex, "Vertex Select");
			ImGui.SameLine();
			DrawSelectionModeButton($"{Icons.Face}##Faces",      SelectionMode.Face,   "Face Select");
		}

		private void DrawSelectionModeButton(string label, SelectionMode mode, string tooltip) =>
			DrawToggleButton(label, SelectionMode == mode, () => SelectionMode = mode, tooltip);

		/// <summary>Draws a button that highlights with <see cref="ImGuiCol.ButtonActive"/> when <paramref name="active"/>,
		/// invoking <paramref name="onClick"/> when clicked while inactive, and showing <paramref name="tooltip"/> on hover.</summary>
		private static void DrawToggleButton(string label, bool active, Action onClick, string tooltip)
		{
			if (active)
				ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive]);

			if (ImGui.Button(label) && !active)
				onClick();

			if (active)
				ImGui.PopStyleColor();

			if (ImGui.IsItemHovered())
				ImGui.SetTooltip(tooltip);
		}

		private void DrawModelSelector()
		{
			if (ApiDataMode)
			{
				string label = SelectedModel is not null ? $"API: {Path.GetFileName(SelectedModel)}" : "API Data";
				ImGui.BeginDisabled();
				ImGui.BeginCombo("##Model", label);
				ImGui.EndDisabled();
				return;
			}

			string preview = SelectedModel is not null ? ModelLabel(SelectedModel) : "- Select a model -";
			string dir = AssetPaths.ModelsDir;
			IEnumerable<(string value, string label)> items = Directory
				.GetFiles(dir, "*.obj", SearchOption.AllDirectories)
				.Select(f => Path.GetRelativePath(dir, f))
				.Select(kind => (kind, kind));

			if (CustomModelPath is not null)
				items = items.Append((CustomModelPath, ModelLabel(CustomModelPath)));

			items = items.OrderBy(item => item.label, StringComparer.OrdinalIgnoreCase);

			DrawAssetCombo("##Model", SelectedModel, preview, items, kind =>
			{
				SelectedModel = kind;
				ModelChangedThisFrame = true;
			});
		}

		private void DrawShaderSelector()
		{
			string preview = SelectedShader ?? "- Select a shader -";
			string dir = AssetPaths.ShadersDir;
			IEnumerable<(string value, string label)> items = Directory
				.GetFiles(dir, "*.vert.glsl", SearchOption.TopDirectoryOnly)
				.Select(f => Path.GetFileName(f).Replace(".vert.glsl", ""))
				.Where(n => !n.StartsWith('_'))
				.Select(name => (value: name, label: name))
				.OrderBy(item => item.label, StringComparer.OrdinalIgnoreCase);

			DrawAssetCombo("##Shader", SelectedShader, preview, items, name =>
			{
				SelectedShader = name;
				ShaderChangedThisFrame = true;
			});
		}

		/// <summary>Draws a combo box listing <paramref name="items"/> as (value, display label) pairs,
		/// highlighting the one matching <paramref name="current"/> and invoking <paramref name="onSelect"/>
		/// with the value of the chosen item.</summary>
		private static void DrawAssetCombo(string id, string? current, string preview, IEnumerable<(string value, string label)> items, Action<string> onSelect)
		{
			if (ImGui.BeginCombo(id, preview))
			{
				foreach ((string value, string label) in items)
				{
					bool selected = value == current;
					if (ImGui.Selectable(label, selected))
						onSelect(value);
					if (selected)
						ImGui.SetItemDefaultFocus();
				}

				ImGui.EndCombo();
			}
		}

		private void DrawImportButton()
		{
			if (ImGui.Button($"{Icons.FolderOpen}##Import"))
			{
				_filePickerMode = FilePickerMode.Model;
				_filePickerModal.Open(filter: ".obj");
			}
			if (ImGui.IsItemHovered())
				ImGui.SetTooltip("Import model from disk");
		}

		private void DrawImportShaderButton()
		{
			if (ImGui.Button($"{Icons.FolderOpen}##ImportShader"))
			{
				_filePickerMode = FilePickerMode.Shader;
				_filePickerModal.Open(filter: ".glsl");
			}
			if (ImGui.IsItemHovered())
				ImGui.SetTooltip("Import shader from disk");
		}

		private void DrawReloadModelButton()
		{
			bool canReload = !ApiDataMode && SelectedModel is not null;
			if (!canReload)
				ImGui.BeginDisabled();

			if (ImGui.Button($"{Icons.Refresh}##ReloadModel") && canReload)
				ReloadModelRequested = true;

			if (!canReload)
				ImGui.EndDisabled();

			if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
				ImGui.SetTooltip("Reload model from disk");
		}

		private void DrawReloadShaderButton()
		{
			bool canReload = SelectedShader is not null;
			if (!canReload)
				ImGui.BeginDisabled();

			if (ImGui.Button($"{Icons.Refresh}##ReloadShader") && canReload)
				ReloadShaderRequested = true;

			if (!canReload)
				ImGui.EndDisabled();

			if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
				ImGui.SetTooltip("Reload shader from disk");
		}

		private void DrawLayoutButton()
		{
			bool canOpen = LayoutDataProvider is not null && LayoutDataProvider() is not null;
			if (!canOpen)
				ImGui.BeginDisabled();

			if (ImGui.Button("Layout") && canOpen)
			{
				var data = LayoutDataProvider!()!.Value;
				_layoutModal.Open(data.meta, data.channels);
			}

			if (!canOpen)
				ImGui.EndDisabled();

			if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
			{
				string tooltip = canOpen ? "Edit vertex attribute mapping" : "Waiting for mesh to load...";
				ImGui.SetTooltip(tooltip);
			}
		}

		private static void DrawKeybindsPopup()
		{
			if (!ImGui.BeginPopup("##keybinds_popup", ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize))
				return;

			ImGuiTableFlags tableFlags = ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoHostExtendX;
			if (ImGui.BeginTable("##keybinds_table", 2, tableFlags))
			{
				ImGui.TableSetupColumn("key",  ImGuiTableColumnFlags.WidthFixed);
				ImGui.TableSetupColumn("desc", ImGuiTableColumnFlags.WidthStretch);

				ImGui.TableNextRow(); ImGui.TableSetColumnIndex(0);
				ImGui.SeparatorText("Camera");
				ImGui.TableNextColumn();
				ImGui.SeparatorText("");

				DrawKeybindRow("Left-drag",    "Orbit");
				DrawKeybindRow("Middle-drag",  "Pan");
				DrawKeybindRow("Scroll",       "Zoom");
				DrawKeybindRow("F",            "Focus on selection / entity");
				DrawKeybindRow("R",            "Reset camera");

				ImGui.TableNextRow(); ImGui.TableSetColumnIndex(0);
				ImGui.SeparatorText("Viewport");
				ImGui.TableNextColumn();
				ImGui.SeparatorText("");

				DrawKeybindRow("Z", "Cycle render mode (Solid / Wireframe / Points)");
				DrawKeybindRow("N", "Toggle normals overlay");
				DrawKeybindRow("B", "Toggle backface culling");

				ImGui.TableNextRow(); ImGui.TableSetColumnIndex(0);
				ImGui.SeparatorText("Selection");
				ImGui.TableNextColumn();
				ImGui.SeparatorText("");

				DrawKeybindRow("Tab",          "Cycle selection mode (Vertex / Face)");
				DrawKeybindRow("Right-click",  "Pick vertex or face");

				ImGui.TableNextRow(); ImGui.TableSetColumnIndex(0);
				ImGui.SeparatorText("General");
				ImGui.TableNextColumn();
				ImGui.SeparatorText("");

				DrawKeybindRow("Q", "Quit");

				ImGui.EndTable();
			}

			ImGui.EndPopup();
		}

		private static void DrawKeybindRow(string key, string description)
		{
			ImGui.TableNextRow();
			ImGui.TableSetColumnIndex(0);
			ImGui.TextDisabled(key);
			ImGui.TableNextColumn();
			ImGui.TextUnformatted(description);
		}

		private void ImportModel(string sourcePath)
		{
			string modelsDir = AssetPaths.ModelsDir;
			string dest = Path.Combine(modelsDir, Path.GetFileName(sourcePath));
			File.Copy(sourcePath, dest, overwrite: true);

			string metaSrc = sourcePath + ".scene";
			if (File.Exists(metaSrc))
				File.Copy(metaSrc, dest + ".scene", overwrite: true);

			SelectedModel = Path.GetRelativePath(modelsDir, dest);
			ModelChangedThisFrame = true;
		}

		private static void ImportShader(string sourcePath)
		{
			string shadersDir = AssetPaths.ShadersDir;
			string dest = Path.Combine(shadersDir, Path.GetFileName(sourcePath));
			File.Copy(sourcePath, dest, overwrite: true);

			// Also import the matching vert/frag counterpart if it sits beside the picked file.
			string? sourceDir = Path.GetDirectoryName(sourcePath);
			string fileName = Path.GetFileName(sourcePath);
			string? counterpartName = fileName.EndsWith(".vert.glsl")
				? fileName[..^".vert.glsl".Length] + ".frag.glsl"
				: fileName.EndsWith(".frag.glsl")
					? fileName[..^".frag.glsl".Length] + ".vert.glsl"
					: null;

			if (counterpartName is not null && sourceDir is not null)
			{
				string counterpartSource = Path.Combine(sourceDir, counterpartName);
				if (File.Exists(counterpartSource))
					File.Copy(counterpartSource, Path.Combine(shadersDir, counterpartName), overwrite: true);
			}
		}

		private string ModelLabel(string kind) =>
			kind == CustomModelPath ? Path.GetFileName(kind) + " (custom)" : kind;

		private static void ToolbarText(string text, Vector4 color)
		{
			ImGui.SetCursorPosY((ImGui.GetWindowHeight() - ImGui.GetTextLineHeight()) * 0.5f);
			ImGui.PushStyleColor(ImGuiCol.Text, color);
			ImGui.TextUnformatted(text);
			ImGui.PopStyleColor();
		}

		private static void ToolbarSeparator()
		{
			float verticalOffset = 10f - ImGui.GetStyle().FramePadding.Y;
			ImGui.SameLine();
			ImGui.SetCursorPosY(ImGui.GetCursorPosY() - verticalOffset);
			ImGui.Separator();
			ImGui.SetCursorPosY(ImGui.GetCursorPosY() + verticalOffset);
			ImGui.SameLine();
		}
	}
}
