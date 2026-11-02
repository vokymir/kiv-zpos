using System.Diagnostics.CodeAnalysis;

namespace Visualizer
{
	/// <summary>Keyed cache of <see cref="AssetMetadata"/> instances, shared by <see cref="MeshLibrary"/>
	/// and <see cref="AnimationLibrary"/>.</summary>
	public sealed class MetadataCache
	{
		private readonly Dictionary<string, AssetMetadata> _metadata = [];

		public AssetMetadata this[string key]
		{
			get => _metadata[key];
			set => _metadata[key] = value;
		}

		public bool TryGetValue(string key, [MaybeNullWhen(false)] out AssetMetadata value) => _metadata.TryGetValue(key, out value);

		/// <summary>Returns the cached metadata for <paramref name="key"/>, or <see langword="null"/> if not loaded.</summary>
		public AssetMetadata? Get(string key) => _metadata.TryGetValue(key, out AssetMetadata? m) ? m : null;

		public void Clear() => _metadata.Clear();
	}
}
