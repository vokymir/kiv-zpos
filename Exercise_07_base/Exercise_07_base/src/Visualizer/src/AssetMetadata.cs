using System.Text.Json;
using System.Text.Json.Serialization;

namespace Visualizer
{
	/// <summary>JSON root of the .scene sidecar file. All scene state lives under <see cref="Scene"/>.</summary>
	public sealed class AssetMetadata
	{
		private static readonly JsonSerializerOptions _jsonOptions = new()
		{
			WriteIndented = true,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		};

		public SceneConfig? Scene { get; set; }

		/// <summary>Ensures <see cref="Scene"/> and <see cref="SceneConfig.Model"/> exist and returns the model config.</summary>
		public SceneModelConfig EnsureModel()
		{
			Scene ??= new SceneConfig();
			return Scene.Model ??= new SceneModelConfig();
		}

		// ── I/O ──────────────────────────────────────────────────────────────

		/// <summary>
		/// Returns the path of the .scene file for <paramref name="assetPath"/>
		/// (a file or a directory).
		/// </summary>
		public static string MetaPath(string assetPath) => assetPath + ".scene";

		/// <summary>
		/// Loads the .scene file next to <paramref name="assetPath"/>.
		/// Creates and saves a default <see cref="AssetMetadata"/> if no file exists.
		/// </summary>
		public static AssetMetadata Load(string assetPath)
		{
			string metaPath = MetaPath(assetPath);

			if (File.Exists(metaPath))
			{
				try
				{
					string json = File.ReadAllText(metaPath);
					return JsonSerializer.Deserialize<AssetMetadata>(json, _jsonOptions)
						?? CreateAndSave(assetPath, metaPath);
				}
				catch (Exception ex) when (ex is JsonException or IOException)
				{
					Console.Error.WriteLine($"[AssetMetadata] Failed to read {metaPath}: {ex.Message}");
					return CreateAndSave(assetPath, metaPath);
				}
			}

			return CreateAndSave(assetPath, metaPath);
		}

		/// <summary>Writes this metadata back to its .scene file.</summary>
		public void Save(string assetPath)
		{
			string metaPath = MetaPath(assetPath);
			try
			{
				File.WriteAllText(metaPath, JsonSerializer.Serialize(this, _jsonOptions));
			}
			catch (IOException ex)
			{
				Console.Error.WriteLine($"[AssetMetadata] Failed to write {metaPath}: {ex.Message}");
			}
		}

		private static AssetMetadata CreateAndSave(string assetPath, string metaPath)
		{
			var meta = new AssetMetadata();
			meta.EnsureModel().DisplayName = Path.GetFileNameWithoutExtension(assetPath);

			try
			{
				File.WriteAllText(metaPath, JsonSerializer.Serialize(meta, _jsonOptions));
			}
			catch (IOException ex)
			{
				Console.Error.WriteLine($"[AssetMetadata] Failed to create {metaPath}: {ex.Message}");
			}

			return meta;
		}
	}
}
