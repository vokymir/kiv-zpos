using Core.IO;
using Renderer.Components;

namespace Visualizer
{
	/// <summary>Discovers and loads mesh animation sequences from subdirectories of Assets/Models/.</summary>
	public sealed class AnimationLibrary : IDisposable
	{
		private readonly IMeshImporter _importer;
		private readonly Dictionary<string, Mesh[]> _cache = [];
		private readonly MetadataCache _metadata = new();

		public AnimationLibrary(IMeshImporter importer) { _importer = importer; }

		private static string AnimationsRoot => AssetPaths.ModelsDir;

		/// <summary>Returns names of subdirectories that contain at least one .obj file.</summary>
		public static IReadOnlyList<string> DiscoverSequences()
		{
			string root = AnimationsRoot;
			if (!Directory.Exists(root))
				return [];

			return Directory
				.GetDirectories(root, "*", SearchOption.AllDirectories)
				.Where(d => Directory.GetFiles(d, "*.obj").Length > 0)
				.Select(d => Path.GetRelativePath(root, d))
				.Order()
				.ToList();
		}

		/// <summary>Returns the frames for <paramref name="sequenceName"/>, loading and caching them on first access.</summary>
		public Mesh[] Get(string sequenceName)
		{
			if (_cache.TryGetValue(sequenceName, out Mesh[]? frames))
				return frames;

			string dir = Path.Combine(AnimationsRoot, sequenceName);
			_metadata[sequenceName] = AssetMetadata.Load(dir);
			frames = Load(sequenceName);
			_cache[sequenceName] = frames;
			return frames;
		}

		private Mesh[] Load(string sequenceName)
		{
			string dir = Path.Combine(AnimationsRoot, sequenceName);
			string[] files = Directory
				.GetFiles(dir, "*.obj")
				.OrderBy(f => ExtractNumber(Path.GetFileNameWithoutExtension(f)))
				.ThenBy(f => f)
				.ToArray();

			// Parse all frames in parallel (ObjImporter is stateless, thread-safe).
			// SetGeometry (OpenGL) must stay on the calling (main) thread.
			var parsed = new Core.IO.MeshData[files.Length];
			Parallel.For(0, files.Length, i => parsed[i] = _importer.Load(files[i]));

			Mesh[] meshes = new Mesh[files.Length];
			for (int i = 0; i < files.Length; i++)
			{
				Mesh mesh = new();
				mesh.SetGeometry(parsed[i].Vertices, parsed[i].Indices);
				meshes[i] = mesh;
			}
			return meshes;
		}

		private static int ExtractNumber(string name)
		{
			// Extract leading or trailing integer for natural sort (e.g. "frame_001" → 1, "042" → 42)
			var digits = new string(name.Where(char.IsDigit).ToArray());
			return digits.Length > 0 && int.TryParse(digits, out int n) ? n : int.MaxValue;
		}

		/// <summary>Returns the <see cref="AssetMetadata"/> for <paramref name="sequenceName"/>, or <see langword="null"/> if not yet loaded.</summary>
		public AssetMetadata? GetMetadata(string sequenceName) => _metadata.Get(sequenceName);

		public void Dispose()
		{
			foreach (string key in _cache.Keys.ToList())
			{
				foreach (Mesh m in _cache[key])
					m.Dispose();
				_cache.Remove(key);
			}
			_metadata.Clear();
		}
	}
}
