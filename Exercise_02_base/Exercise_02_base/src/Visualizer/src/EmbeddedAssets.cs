using System.Reflection;

namespace Visualizer
{
	internal static class EmbeddedAssets
	{
		private static readonly (string Resource, string Path)[] Assets =
		[
			("Visualizer.Assets.Shaders.default.vert.glsl",                    "Assets/Shaders/default.vert.glsl"),
			("Visualizer.Assets.Shaders.default.frag.glsl",                    "Assets/Shaders/default.frag.glsl"),
			("Visualizer.Assets.Shaders._grid.vert.glsl",                      "Assets/Shaders/_grid.vert.glsl"),
			("Visualizer.Assets.Shaders._grid.frag.glsl",                      "Assets/Shaders/_grid.frag.glsl"),
			("Visualizer.Assets.Shaders._normalLines.vert.glsl",               "Assets/Shaders/_normalLines.vert.glsl"),
			("Visualizer.Assets.Shaders._normalLines.frag.glsl",               "Assets/Shaders/_normalLines.frag.glsl"),
			("Visualizer.Assets.Shaders.normals.vert.glsl",                    "Assets/Shaders/normals.vert.glsl"),
			("Visualizer.Assets.Shaders.normals.frag.glsl",                    "Assets/Shaders/normals.frag.glsl"),
			("Visualizer.Assets.Fonts.Noto.NotoSansMNerdFontPropo-Regular.ttf","Assets/Fonts/Noto/NotoSansMNerdFontPropo-Regular.ttf"),
		];

		/// <summary>
		/// Extracts embedded shaders and fonts to <c>Assets/</c> under <see cref="AppContext.BaseDirectory"/>
		/// if they are not already present. User-provided assets (models, custom shaders) are never touched.
		/// </summary>
		internal static void Extract()
		{
			Assembly asm = Assembly.GetExecutingAssembly();
			string baseDir = AppContext.BaseDirectory;

			foreach (var (resource, relativePath) in Assets)
			{
				string fullPath = Path.Combine(baseDir, relativePath);
				if (File.Exists(fullPath))
					continue;

				Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

				using Stream stream = asm.GetManifestResourceStream(resource)
					?? throw new InvalidOperationException($"Embedded resource not found: {resource}");
				using FileStream file = File.Create(fullPath);
				stream.CopyTo(file);
			}
		}
	}
}
