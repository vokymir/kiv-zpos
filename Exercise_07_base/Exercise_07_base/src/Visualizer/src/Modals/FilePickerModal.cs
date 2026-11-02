using ImGuiNET;
using System.Numerics;

namespace Visualizer.Modals
{
	/// <summary>
	/// ImGui modal file browser. Supports navigating directories and picking a single file.
	/// </summary>
	public sealed class FilePickerModal
	{
		private const string PopupId = "Open File###FilePickerModal";

		private bool    _pendingOpen;
		private string  _currentDir  = "/";
		private string  _selected    = "";
		private string  _filter      = "";
		private string? _lastDir;

		// Result accessible after PickedThisFrame is true.
		public bool    PickedThisFrame { get; private set; }
		public string? PickedPath      { get; private set; }

		/// <summary>Opens the modal on the next <see cref="Draw"/> call.</summary>
		/// <param name="startDir">Directory to start in; falls back to home then root.</param>
		/// <param name="filter">Optional file extension filter, e.g. ".obj".</param>
		public void Open(string? startDir = null, string filter = "")
		{
			_filter   = filter;
			_selected = "";

			string dir = startDir
				?? _lastDir
				?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

			_currentDir = Directory.Exists(dir) ? dir : "/";
			_pendingOpen = true;
		}

		/// <summary>Call once per frame inside an active ImGui frame.</summary>
		public void Draw()
		{
			PickedThisFrame = false;
			PickedPath      = null;

			if (_pendingOpen)
			{
				ImGui.OpenPopup(PopupId);
				_pendingOpen = false;
			}

			Vector2 center = ImGui.GetMainViewport().GetCenter();
			ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
			ImGui.SetNextWindowSize(new Vector2(600, 420), ImGuiCond.Appearing);

			bool open = true;
			if (!ImGui.BeginPopupModal(PopupId, ref open, ImGuiWindowFlags.NoScrollbar))
				return;

			DrawBreadcrumb();
			ImGui.Separator();
			DrawEntries();
			ImGui.Separator();
			DrawFooter();

			ImGui.EndPopup();
		}

		// ── Breadcrumb ────────────────────────────────────────────────────────

		private void DrawBreadcrumb()
		{
			// Split path into segments and draw clickable buttons.
			string[] parts = _currentDir.Split(Path.DirectorySeparatorChar,
				StringSplitOptions.RemoveEmptyEntries);

			if (ImGui.SmallButton("/"))
				Navigate("/");

			for (int i = 0; i < parts.Length; i++)
			{
				ImGui.SameLine(0, 2);
				ImGui.TextDisabled(Icons.BreadcrumbSeparator);
				ImGui.SameLine(0, 2);

				string segment = parts[i];
				string fullPath = "/" + string.Join("/", parts[..  (i + 1)]);

				if (ImGui.SmallButton(segment + $"##{i}"))
					Navigate(fullPath);
			}
		}

		// ── Entry list ───────────────────────────────────────────────────────

		private void DrawEntries()
		{
			// Reserve space for the footer row.
			float footerHeight = ImGui.GetFrameHeightWithSpacing() * 2 + ImGui.GetStyle().ItemSpacing.Y;
			ImGui.BeginChild("##entries", new Vector2(0, -footerHeight), ImGuiChildFlags.None);

			// Parent directory link
			string? parent = Directory.GetParent(_currentDir)?.FullName;
			if (parent is not null)
			{
				if (ImGui.Selectable(".. (up)", false, ImGuiSelectableFlags.None))
					Navigate(parent);
			}

			// Directories
			try
			{
				string[] dirs = [.. Directory.GetDirectories(_currentDir).OrderBy(d => d, StringComparer.OrdinalIgnoreCase)];

				foreach (string dir in dirs)
				{
					string name = Path.GetFileName(dir);
					bool sel = _selected == dir;
					if (ImGui.Selectable($"{Icons.FolderOpen}  {name}##{dir}", sel,
						ImGuiSelectableFlags.None))
					{
						_selected = dir;
					}
					if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
						Navigate(dir);
				}
			}
			catch (UnauthorizedAccessException) { ImGui.TextDisabled("(access denied)"); }

			// Files
			try
			{
				string[] files = [.. Directory.GetFiles(_currentDir)
					.Where(f => string.IsNullOrEmpty(_filter) ||
						Path.GetExtension(f).Equals(_filter, StringComparison.OrdinalIgnoreCase))
					.OrderBy(f => f, StringComparer.OrdinalIgnoreCase)];

				foreach (string file in files)
				{
					string name = Path.GetFileName(file);
					bool sel = _selected == file;
					if (ImGui.Selectable(name + $"##{file}", sel,
						ImGuiSelectableFlags.None))
					{
						_selected = file;
					}
					if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
						Confirm(file);
				}
			}
			catch (UnauthorizedAccessException) { }

			ImGui.EndChild();
		}

		// ── Footer ───────────────────────────────────────────────────────────

		private void DrawFooter()
		{
			// Show selected file name (read-only preview)
			string display = string.IsNullOrEmpty(_selected)
				? ""
				: Path.GetFileName(_selected);

			ImGui.SetNextItemWidth(-1);
			ImGui.InputText("##selpath", ref display, 512, ImGuiInputTextFlags.ReadOnly);

			// Determine whether "Open" should be enabled (must be a file, not a dir)
			bool canOpen = File.Exists(_selected);

			if (!canOpen) ImGui.BeginDisabled();
			if (ImGui.Button("Open", new Vector2(100, 0)))
				Confirm(_selected);
			if (!canOpen) ImGui.EndDisabled();

			ImGui.SameLine();
			if (ImGui.Button("Cancel", new Vector2(100, 0)))
				ImGui.CloseCurrentPopup();
		}

		// ── Helpers ──────────────────────────────────────────────────────────

		private void Navigate(string dir)
		{
			if (Directory.Exists(dir))
			{
				_currentDir = dir;
				_selected   = "";
			}
		}

		private void Confirm(string file)
		{
			if (!File.Exists(file)) return;
			PickedThisFrame = true;
			PickedPath      = file;
			_lastDir        = Path.GetDirectoryName(file);
			ImGui.CloseCurrentPopup();
		}
	}
}
