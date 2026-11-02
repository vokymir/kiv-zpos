using System.Collections.Concurrent;
using System.Linq;
using Core.Structures;
using Renderer.Components;
using Renderer.Objects;
using Renderer.Wrappers.Model;
using Visualizer.Scripts;

namespace Visualizer.Controllers
{
	/// <summary>
	/// Owns the API-provided mesh/animation geometry (<see cref="SetMeshCommand"/> /
	/// <see cref="SetAnimationCommand"/>) and drains the command queue used by the
	/// embedded HTTP/IPC API to drive the visualizer from external code.
	/// </summary>
	internal sealed class ApiMeshController
	{
		private Mesh? _apiMesh;
		private Mesh[]? _apiAnimFrames;
		private Vertex[]? _apiMeshVertices;
		private int[]? _apiMeshIndices;
		private FrameData[]? _apiAnimFrameData;

		public bool HasApiData => _apiMesh is not null || _apiAnimFrames is not null;

		public void Dispose()
		{
			_apiMesh?.Dispose();
			_apiMesh = null;
			DisposeAnimFrames();
		}

		private void DisposeAnimFrames()
		{
			if (_apiAnimFrames is null) return;
			foreach (Mesh f in _apiAnimFrames) f.Dispose();
			_apiAnimFrames = null;
		}

		/// <summary>
		/// Processes all queued visualizer commands, updating <paramref name="entity"/> and
		/// <paramref name="animController"/> with API-provided geometry. When a command switches
		/// to a different scene, <paramref name="loadApiSceneMetadata"/> is invoked to load the
		/// new scene's sidecar before the mesh is assigned, and <paramref name="applyApiSceneConfig"/>
		/// is invoked afterwards to restore that scene's saved material/camera/lighting/selection.
		/// </summary>
		public void DrainCommandQueue(
			ConcurrentQueue<IVisualizerCommand> commandQueue,
			Entity entity,
			MeshAnimationController animController,
			MeshLibrary meshLibrary,
			SceneState sceneState,
			Action<string> loadApiSceneMetadata,
			Action applyApiSceneConfig)
		{
			while (commandQueue.TryDequeue(out IVisualizerCommand? cmd))
			{
				switch (cmd)
				{
					case SetMeshCommand m:
					{
						animController.Unload();
						DisposeAnimFrames();
						_apiAnimFrameData = null;
						_apiMeshVertices = m.Vertices;
						_apiMeshIndices = m.Indices;

						bool sceneChanged = m.SceneName is not null && m.SceneName != sceneState.CurrentModel;
						if (sceneChanged)
						{
							loadApiSceneMetadata(m.SceneName!);
							animController.Unload();
							sceneState.CurrentAnimation = null;
						}

						AssetMetadata? meshMeta = meshLibrary.GetMetadata(sceneState.CurrentModel);
						List<VertexChannelSpec>? layout = meshMeta?.Scene?.Model?.VertexLayout;
						if (layout is null && m.Layout is not null)
						{
							layout = m.Layout;
							if (meshMeta is not null)
								meshMeta.EnsureModel().VertexLayout = m.Layout;
						}
						Mesh apiMesh = new();
						apiMesh.SetGeometry(m.Vertices, m.Indices, layout);
						_apiMesh?.Dispose();
						_apiMesh = apiMesh;
						entity.Mesh = _apiMesh;

						if (sceneChanged)
						{
							// Apply with the new mesh already in place so the saved selection
							// can be validated against its actual vertex/face counts.
							applyApiSceneConfig();
							animController.Unload(); // a restored scene may have loaded a file-based animation
							sceneState.CurrentAnimation = null;
							entity.Mesh = _apiMesh;
						}
						break;
					}

					case SetAnimationCommand a:
					{
						animController.Unload();
						_apiMesh?.Dispose();
						_apiMesh = null;
						_apiMeshVertices = null;
						_apiMeshIndices = null;
						DisposeAnimFrames();
						_apiAnimFrameData = a.Frames;

						bool animSceneChanged = a.SceneName is not null && a.SceneName != sceneState.CurrentModel;
						if (animSceneChanged)
						{
							loadApiSceneMetadata(a.SceneName!);
							animController.Unload();
							sceneState.CurrentAnimation = null;
						}

						AssetMetadata? animMeta = meshLibrary.GetMetadata(sceneState.CurrentModel);
						List<VertexChannelSpec>? sceneLayout = animMeta?.Scene?.Model?.VertexLayout;
						if (sceneLayout is null)
						{
							List<VertexChannelSpec>? apiLayout = a.Frames.Select(f => f.Layout).FirstOrDefault(l => l is not null);
							if (apiLayout is not null)
							{
								sceneLayout = apiLayout;
								if (animMeta is not null)
									animMeta.EnsureModel().VertexLayout = apiLayout;
							}
						}
						Mesh[] frames = new Mesh[a.Frames.Length];
						for (int i = 0; i < a.Frames.Length; i++)
						{
							frames[i] = new Mesh();
							frames[i].SetGeometry(a.Frames[i].Vertices, a.Frames[i].Indices, sceneLayout ?? a.Frames[i].Layout);
						}
						_apiAnimFrames = frames;
						animController.LoadSequence(_apiAnimFrames);
						entity.Mesh = animController.CurrentMesh;

						if (animSceneChanged)
						{
							// Apply with the API frames already loaded so the saved selection
							// can be validated against the actual vertex/face counts, and so a
							// saved frame index/playback state can be restored onto these frames.
							applyApiSceneConfig();
							if (sceneState.CurrentAnimation is not null)
							{
								// The scene loaded a named file-based animation; discard it and
								// fall back to the API-provided frames instead.
								sceneState.CurrentAnimation = null;
								animController.LoadSequence(_apiAnimFrames);
							}
							entity.Mesh = animController.CurrentMesh;
						}
						break;
					}

					case PlayCommand:
						animController.Play();
						break;

					case PauseCommand:
						animController.Pause();
						break;

					case StopCommand:
						animController.Reset();
						break;
				}
			}
		}

		/// <summary>
		/// Re-uploads the cached API mesh/animation geometry using the vertex layout currently
		/// stored in the active scene's metadata. Used when the layout is edited via the UI,
		/// since API meshes have no source file to reload from.
		/// </summary>
		public void ReapplyApiVertexLayout(MeshLibrary meshLibrary, string currentModel)
		{
			List<VertexChannelSpec>? layout = meshLibrary.GetMetadata(currentModel)?.Scene?.Model?.VertexLayout;

			if (_apiMesh is not null && _apiMeshVertices is not null && _apiMeshIndices is not null)
			{
				_apiMesh.SetGeometry(_apiMeshVertices, _apiMeshIndices, layout);
			}
			else if (_apiAnimFrames is not null && _apiAnimFrameData is not null)
			{
				for (int i = 0; i < _apiAnimFrames.Length; i++)
					_apiAnimFrames[i].SetGeometry(_apiAnimFrameData[i].Vertices, _apiAnimFrameData[i].Indices, layout ?? _apiAnimFrameData[i].Layout);
			}
		}
	}
}
