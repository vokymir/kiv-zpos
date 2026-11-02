using System.Runtime.InteropServices;
using ImGuiNET;

namespace Visualizer
{
	/// <summary>Cache of loaded ImGui fonts keyed by filename. Fonts must be added before the first ImGui frame (before <see cref="ImGui.NewFrame"/>).</summary>
	public sealed class FontLibrary
	{
		private readonly Dictionary<string, ImFontPtr> _fonts = [];

		// ImGui keeps a raw pointer to each font's glyph ranges array for use during Fonts.Build(),
		// which runs after this method returns - the array must stay pinned for the FontLibrary's lifetime.
		private readonly List<GCHandle> _pinnedRanges = [];

		/// <summary>Loads and caches a font from <c>Assets/Fonts/</c>. Call before the first ImGui frame.</summary>
		/// <param name="relativePath">Path relative to <c>Assets/Fonts/</c>, e.g. <c>"Noto/NotoSansMNerdFontPropo-Regular.ttf"</c>.</param>
		/// <param name="sizePixels">Font size in pixels.</param>
		/// <param name="glyphRanges">Optional extra glyph ranges appended after Latin (0x0020–0x00FF). Each range is a [lo, hi] pair; the array must end with 0.</param>
		public ImFontPtr Get(string relativePath, float sizePixels, ushort[]? glyphRanges = null)
		{
			string key = $"{relativePath}@{sizePixels}";

			if (_fonts.TryGetValue(key, out ImFontPtr font))
				return font;

			font = LoadFont(relativePath, sizePixels, glyphRanges);
			_fonts.Add(key, font);
			return font;
		}

		private ImFontPtr LoadFont(string relativePath, float sizePixels, ushort[]? extraRanges)
		{
			string path = Path.Combine(AssetPaths.FontsDir, relativePath);

			if (!File.Exists(path))
				throw new FileNotFoundException($"Font file not found: {path}");

			if (extraRanges is null)
				return ImGui.GetIO().Fonts.AddFontFromFileTTF(path, sizePixels);

			// Build range array: Latin (0x0020–0x00FF) + caller-supplied ranges + terminator
			ushort[] ranges = [0x0020, 0x00FF, .. extraRanges];
			// Ensure the array is null-terminated (caller should include 0, but be safe)
			if (ranges[^1] != 0)
				ranges = [.. ranges, 0];

			GCHandle handle = GCHandle.Alloc(ranges, GCHandleType.Pinned);
			_pinnedRanges.Add(handle);
			return ImGui.GetIO().Fonts.AddFontFromFileTTF(path, sizePixels, default, handle.AddrOfPinnedObject());
		}
	}
}
