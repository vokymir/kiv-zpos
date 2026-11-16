using Core.IO;

namespace Visualizer
{
	internal static class MeshImporterFactory
	{
		public static readonly IReadOnlyList<string> SupportedFormats = ["obj"];

		public static IMeshImporter Resolve(string format) => format.ToLowerInvariant() switch
		{
			"obj" => new ObjImporter(),
			_ => throw new ArgumentException($"Unknown format '{format}'. Supported: {string.Join(", ", SupportedFormats)}"),
		};
	}
}
