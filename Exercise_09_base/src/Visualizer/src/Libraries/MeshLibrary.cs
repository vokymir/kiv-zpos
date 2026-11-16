using Core.IO;
using Core.PostProcessing;
using Renderer.Components;

namespace Visualizer
{
	/// <summary>Cache of loaded <see cref="Mesh"/> objects keyed by filename. Loads on first access.</summary>
	/// <remarks>
	/// Parsing runs on a background thread; <see cref="Get"/> returns <see langword="null"/> while the
	/// initial background task is in flight, and the fully-uploaded <see cref="Mesh"/> on the next call
	/// after it completes. During a <see cref="BeginReload"/>, <see cref="Get"/> keeps returning the
	/// previous mesh until the new one is ready, then swaps and disposes the old one — no blank frame.
	/// <see cref="SetGeometry"/> (OpenGL) is always called on the thread that calls <see cref="Get"/>,
	/// which must be the main/render thread.
	/// </remarks>
	public sealed class MeshLibrary : IDisposable
	{
		private readonly Dictionary<string, Mesh> _meshes = [];
		private readonly Dictionary<string, Task<MeshData>> _pending = [];
		private readonly MetadataCache _metadata = new();

		/// <summary>Returns <see langword="true"/> while any background parse task is still running.</summary>
		public bool IsAnyPending => _pending.Count > 0;

		/// <summary>
		/// Returns the cached <see cref="Mesh"/> for <paramref name="kind"/>, or <see langword="null"/>
		/// if parsing is still in progress. Starts the background load on the first call.
		/// Must be called from the main/render thread.
		/// </summary>
		public Mesh? Get(string kind)
		{
			_meshes.TryGetValue(kind, out Mesh? existing);

			if (!_pending.TryGetValue(kind, out Task<MeshData>? task))
			{
				if (existing is not null)
					return existing; // no reload in progress

				string path = AssetPaths.ModelPath(kind);
				AssetMetadata meta = AssetMetadata.Load(path);
				SceneModelConfig model = meta.EnsureModel();
				model.Format ??= Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
				_metadata[kind] = meta;
				IMeshImporter importer = MeshImporterFactory.Resolve(model.Format);
				task = Task.Run(() =>
				{
					MeshData data = importer.Load(path);
					NormalsGenerator.Apply(data.Vertices, data.Indices, model.NormalsMode);
					return data;
				});
				_pending[kind] = task;
			}

			if (task.IsFaulted)
			{
				Console.Error.WriteLine($"[MeshLibrary] Failed to load '{kind}': {task.Exception?.InnerException?.Message}");
				_pending.Remove(kind);
				return existing;
			}

			if (!task.IsCompletedSuccessfully)
				return existing; // return old mesh (or null on initial load) while task runs

			// Task finished — upload to GPU on the calling (main) thread, then swap.
			MeshData data = task.Result;
			Mesh newMesh = new Mesh();
			newMesh.SetGeometry(data, _metadata[kind].Scene?.Model?.VertexLayout);
			_pending.Remove(kind);
			existing?.Dispose();
			_meshes[kind] = newMesh;
			return newMesh;
		}

		/// <summary>Returns the <see cref="AssetMetadata"/> for <paramref name="kind"/>, or <see langword="null"/> if not yet loaded.</summary>
		public AssetMetadata? GetMetadata(string kind) => _metadata.Get(kind);

		/// <summary>Replaces the cached metadata for <paramref name="kind"/> with a freshly loaded instance.</summary>
		public void SetMetadata(string kind, AssetMetadata meta) => _metadata[kind] = meta;

		/// <summary>
		/// Starts a background re-parse so the next completed <see cref="Get"/> call re-uploads
		/// with the current metadata. The old mesh keeps serving until the new one is ready.
		/// </summary>
		public void BeginReload(string kind)
		{
			_pending.Remove(kind);
			// _meshes entry is intentionally kept so Get() can return it while the new task runs.
			string path = AssetPaths.ModelPath(kind);
			// Reuse the cached metadata (it may carry unsaved edits, e.g. from the Vertex Layout modal)
			// instead of reloading from disk, which would discard them.
			AssetMetadata meta = _metadata.TryGetValue(kind, out AssetMetadata? existing) ? existing : AssetMetadata.Load(path);
			SceneModelConfig model = meta.EnsureModel();
			model.Format ??= Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
			_metadata[kind] = meta;
			IMeshImporter importer = MeshImporterFactory.Resolve(model.Format);
			_pending[kind] = Task.Run(() =>
			{
				MeshData data = importer.Load(path);
				NormalsGenerator.Apply(data.Vertices, data.Indices, model.NormalsMode);
				return data;
			});
		}

		/// <summary>Returns <see langword="true"/> while a background task (reload or normals regen) is running.</summary>
		public bool IsReloading(string kind) => _pending.ContainsKey(kind) && _meshes.ContainsKey(kind);

		/// <summary>
		/// Starts a background normals regeneration. The old mesh keeps serving until the new one is
		/// ready; <see cref="Get"/> swaps and disposes the old one when the task completes.
		/// No-op if a reload is already in flight (the reload will apply the correct normals mode).
		/// </summary>
		public void BeginRegenerateNormals(string kind)
		{
			if (!_meshes.TryGetValue(kind, out Mesh? mesh))
				return;
			if (IsReloading(kind))
				return;

			MeshData snapshot = new(mesh.Vertices, mesh.Indices, mesh.NormalsSource);
			NormalsMode mode = _metadata[kind].Scene?.Model?.NormalsMode ?? NormalsMode.Flat; // model guaranteed non-null after Get()
			_pending[kind] = Task.Run(() =>
			{
				NormalsGenerator.Apply(snapshot.Vertices, snapshot.Indices, mode);
				return snapshot;
			});
		}

        public void Dispose()
		{
			foreach (string key in _meshes.Keys.ToList())
			{
				_meshes[key].Dispose();
				_meshes.Remove(key);
			}
			_pending.Clear();
			_metadata.Clear();
		}
	}
}
