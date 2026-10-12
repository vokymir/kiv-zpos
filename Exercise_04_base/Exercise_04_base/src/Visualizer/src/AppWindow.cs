using Connector;
using Core.IO;
using Core.Structures;
using Core;
using ImGuiNET;
using Renderer.Abstractions;
using Renderer.Components;
using Renderer.Objects;
using Renderer.Wrappers.Model;
using Renderer.Wrappers.Shaders;
using Renderer.Wrappers.Textures;
using Renderer;
using System.Collections.Concurrent;
using System.Numerics;
using Visualizer.Controllers;
using Visualizer.Inspector.Drawers;
using Visualizer.Modals;
using Visualizer.Scripts;
using Visualizer.UI;

namespace Visualizer
{
	/// <summary>
	/// Top-level application window. Wires the render pipeline, orbit camera controller, and UI controller together.
	/// Manages the offscreen <c>RenderTarget</c> for the scene viewport and drives the scene hierarchy.
	/// </summary>
	public class AppWindow : Window
	{
		private readonly UiController _ui = new();

		private readonly Entity        _entity   = new();
		private readonly Camera        _camera   = new();
		private readonly SceneLighting _lighting = new();

		private readonly OrbitCameraController _cameraController = new();
		private readonly NormalsOverlay _normalsOverlay = new();
		private FloorGrid? _floorGrid;
		private readonly MeshAnimationController _animController = new();
		private readonly IMeshImporter _meshImporter;
		private readonly AnimationLibrary _animLibrary;
		private Vector2 _lastMousePosition;
		private InputState _prevInput = new();

		private RenderTarget? _sceneTarget;
		private UiTextureHandle _sceneTextureHandle;

		private SceneHierarchyItem? _entityHierarchyItem;

		private readonly MeshLibrary _meshLibrary;

		private bool _requestModelChange = false;
		private bool _modelReloadPending = false;

		private readonly ConcurrentQueue<IVisualizerCommand> _commandQueue;
		private readonly ApiMeshController _apiMeshController = new();

		private readonly SceneState _sceneState = new();
		private readonly UnsavedChangesGate _unsavedChangesGate = new();
		private readonly SceneDirtyTracker _dirtyTracker = new();
		private readonly SceneConfigController _sceneConfigController;

		internal AppWindow(IMeshImporter importer, string? initialModel = null, ConcurrentQueue<IVisualizerCommand>? commandQueue = null)
		{
			_meshImporter = importer;
			_meshLibrary = new MeshLibrary();
			_animLibrary = new AnimationLibrary(_meshImporter);
			_commandQueue = commandQueue ?? new();
			if (initialModel is not null)
				_sceneState.CurrentModel = initialModel;

			_sceneConfigController = new SceneConfigController(
				_meshLibrary, _animLibrary, _shaderLibrary, _ui, _entity, _camera, _cameraController, _lighting, _animController,
				RebuildSceneHierarchy, () => _dirtyTracker.SnapshotCamera(_cameraController));
		}

		private readonly ShaderLibrary _shaderLibrary = new();
		private readonly FontLibrary _fontLibrary = new();
		private ShaderProgram? _normalsShader;
		private ShaderProgram? _gridShader;

		protected override void OnLoad()
		{
			EmbeddedAssets.Extract();

			Title += $": {API.GetAPIName()} Version: {API.GetVersionString()}";
			RenderFlags.EnableDebug();

			IImGuiBackend imGuiBackend = new ImGuiOpenGLBackend();
			// Nerd Font icon range U+E000–U+F8FF, appended after the default Latin range inside FontLibrary
			ushort[] nerdFontRanges = [0xE000, 0xF8FF, 0];
			ImGuiFrontend imGuiFrontend = new(imGuiBackend, io =>
			{
				ImFontPtr font = _fontLibrary.Get("Noto/NotoSansMNerdFontPropo-Regular.ttf", 20f, nerdFontRanges);
				unsafe { io.NativePtr->FontDefault = font.NativePtr; }
			});
			_ui.Initialize(imGuiFrontend);

			_sceneTarget = new RenderTarget(Width, Height);
			_sceneTextureHandle = _ui.RegisterTexture(_sceneTarget.ColorTexture);
			_ui.SetSceneTexture(_sceneTextureHandle);
			_ui.SetSceneContext(_camera, _entity);
			_ui.SetAnimationController(_animController);
			_ui.SetLoadingProvider(() => _meshLibrary.IsAnyPending);
			_ui.SetLayoutDataProvider(() =>
			{
				if (_sceneState.CurrentModel == "") return null;
				AssetMetadata? meta = _meshLibrary.GetMetadata(_sceneState.CurrentModel);
				if (meta is null) return null;
				string assetPath = AssetPaths.ModelPath(_sceneState.CurrentModel);
				string[] channels = _entity.Mesh?.Vertices is { Length: > 0 } verts
					? [.. verts[0].Channels.Keys]
					: ["position", "normal"];
				return (meta, assetPath, channels);
			});

			/* ---- Material ---- */
			_normalsShader = _shaderLibrary.Get("_normalLines");
			_gridShader    = _shaderLibrary.Get("_grid");
			_floorGrid     = new FloorGrid();
			ShaderProgram shader = _shaderLibrary.Get("default");
			_entity.Material = SceneConfigController.CreateDefaultMaterial(shader);

			/* ---- Transforms ---- */
			_entity.Transforms.Position = new(0f);
			_entity.Transforms.Scale    = new(1f);

			/* ---- Camera ---- */
			_camera.FarPlane  = 1500.0f;
			_camera.NearPlane = 0.01f;
			_cameraController.DesiredTarget   = Vector3.Zero;
			_cameraController.DesiredDistance = 10f;
			_cameraController.DesiredYaw      = Mathematics.DegreesToRadians(135f);
			_cameraController.DesiredPitch    = Mathematics.DegreesToRadians(-30f);
			_cameraController.SnapToDesired();
			_cameraController.Apply(_camera);
			_dirtyTracker.SnapshotCamera(_cameraController);

			/* ---- Lights ---- */
			_lighting.Ambient.Color     = Vector3.One;
			_lighting.Ambient.Intensity = 0.1f;

			_lighting.AddDirectionalLight(new() { Color = Vector3.One, Intensity = 1.0f, Direction = new Vector3(1f, -1f, 1f) });
			_lighting.AddDirectionalLight(new() { Color = Vector3.One, Intensity = 0.5f, Direction = new Vector3(-1f, -1f, -1f) });

            /* ---- Inspector drawers ---- */
            Inspector.InspectorRegistry.Register(new EntityDrawer { OnModified = () => _sceneState.Modified = true });
			Inspector.InspectorRegistry.Register(new CameraDrawer());
			Inspector.InspectorRegistry.Register(new LightingDrawer { OnModified = () => _sceneState.Modified = true });

			/* ---- Hierarchy actions ---- */
			_ui.OnAddDirectionalLight = () =>
			{
				_lighting.AddDirectionalLight(new DirectionalLight { Color = Vector3.One, Intensity = 1.0f, Direction = new Vector3(0f, -1f, 0f) });
				RebuildSceneHierarchy();
			};
			_ui.OnRemoveDirectionalLight = obj =>
			{
				if (obj is DirectionalLight light)
				{
					_lighting.RemoveDirectionalLight(light);
					RebuildSceneHierarchy();
				}
			};

			/* ---- Scene ---- */
			RebuildSceneHierarchy();

			if (_sceneState.CurrentModel != "")
			{
				_ui.CustomModelPath = _sceneState.CurrentModel;
				_ui.RevertModel(_sceneState.CurrentModel);
				_requestModelChange = true;
			}
		}

		protected override void OnClose()
		{
			_apiMeshController.Dispose();
			_meshLibrary.Dispose();
			_animLibrary.Dispose();
			_shaderLibrary.Dispose();
			_sceneTarget?.Dispose();
			_sceneTarget = null;
			_floorGrid?.Dispose();
			_floorGrid = null;
			_gridShader = null;
			_normalsShader = null;
		}

		protected override void OnUpdate()
		{
			DrainCommandQueue();

			ViewState state = _ui.State;

			// ---- Handle unsaved-changes modal result ----
			if (state.UnsavedChangesResult != UnsavedChangesResult.None)
			{
				if (state.UnsavedChangesResult == UnsavedChangesResult.Save)
				{
					_sceneConfigController.SaveSceneConfig(_sceneState);
					_ui.ShowStatusMessage("Scene Saved");
				}

				var (resolution, model) = _unsavedChangesGate.Resolve(state.UnsavedChangesResult);
				switch (resolution)
				{
					case UnsavedChangesGate.Resolution.Exit:
						_sceneState.Modified = false;
						Close();
						break;

					case UnsavedChangesGate.Resolution.ChangeModel:
						_sceneState.Modified = false;
						_sceneState.CurrentModel = model!;
						_requestModelChange = true;
						break;

					case UnsavedChangesGate.Resolution.RevertModel:
						_ui.RevertModel(_sceneState.CurrentModel);
						break;
				}
			}

			// ---- Model change ----
			if (state.ModelChangedThisFrame && state.SelectedModel is not null)
			{
				if (_unsavedChangesGate.RequestModelChange(state.SelectedModel, _sceneState.Modified && _sceneState.CurrentModel != ""))
				{
					_ui.ShowUnsavedChangesPrompt();
				}
				else
				{
					_sceneState.CurrentModel = state.SelectedModel;
					_requestModelChange = true;
				}
			}

			// Poll the background load each frame until the mesh is ready.
			if (_sceneState.CurrentModel != "" && (_requestModelChange || _meshLibrary.IsReloading(_sceneState.CurrentModel)) && !_animController.HasAnimation)
			{
				Mesh? mesh = _meshLibrary.Get(_sceneState.CurrentModel);
				if (mesh is not null && !_meshLibrary.IsReloading(_sceneState.CurrentModel))
				{
					_entity.Mesh = mesh;
					if (_requestModelChange)
					{
						_sceneConfigController.ApplyMaterialOverrides(_sceneState.CurrentModel);
						AssetMetadata? meta = _meshLibrary.GetMetadata(_sceneState.CurrentModel);
						if (meta?.Scene is SceneConfig scene)
							_sceneConfigController.ApplySceneConfig(scene, _sceneState);
						else
							_cameraController.FocusOnEntity(_entity);
						_sceneState.Modified = false;
						_requestModelChange = false;
					}

					if (_modelReloadPending)
					{
						_ui.ShowStatusMessage("Model Reloaded");
						_modelReloadPending = false;
					}
				}
			}

			if (state.ReloadModelRequested && _sceneState.CurrentModel != "")
			{
				_meshLibrary.BeginReload(_sceneState.CurrentModel);
				_modelReloadPending = true;
			}

			if (state.LayoutChangedThisFrame && _sceneState.CurrentModel != "")
			{
				if (_apiMeshController.HasApiData)
					_apiMeshController.ReapplyApiVertexLayout(_meshLibrary, _sceneState.CurrentModel);
				else
					_meshLibrary.BeginReload(_sceneState.CurrentModel);
			}

			if (state.GenerateNormalsMetaChangedThisFrame && _sceneState.CurrentModel != "")
			{
				_meshLibrary.BeginRegenerateNormals(_sceneState.CurrentModel);
			}

			if (state.ShaderChangedThisFrame && state.SelectedShader is not null)
			{
				ShaderProgram shader = _shaderLibrary.Get(state.SelectedShader);
				Material prev = _entity.Material!;
				Material material = SceneConfigController.CreateDefaultMaterial(shader);
				SceneConfigController.CarryOverMaterialParameters(prev, material);
				_entity.Material = material;
			}

			if (state.ReloadShaderRequested && state.SelectedShader is not null)
			{
				_shaderLibrary.Reload(state.SelectedShader);
				ShaderProgram shader = _shaderLibrary.Get(state.SelectedShader);
				Material prev = _entity.Material!;
				Material material = SceneConfigController.CreateDefaultMaterial(shader);
				SceneConfigController.CarryOverMaterialParameters(prev, material);
				_entity.Material = material;
				_ui.ShowStatusMessage("Shader Reloaded");
			}

			if (state.AnimationChangedThisFrame)
			{
				_sceneState.CurrentAnimation = state.SelectedAnimation;
				if (_sceneState.CurrentAnimation is not null)
				{
					Mesh[] frames = _animLibrary.Get(_sceneState.CurrentAnimation);
					_animController.LoadSequence(frames);
					_entity.Mesh = _animController.CurrentMesh;
					if (_entity.Mesh is not null)
						_cameraController.FocusOnEntity(_entity);
				}
				else
				{
					_animController.Unload();
					_entity.Mesh = null;
				}
			}

			if (_animController.HasAnimation)
			{
				_animController.Update(DeltaTime);
				_entity.Mesh = _animController.CurrentMesh;
			}

			if (state.SaveSceneRequested && _sceneState.CurrentModel != "")
			{
				_sceneConfigController.SaveSceneConfig(_sceneState);
				_sceneState.Modified = false;
				_ui.ShowStatusMessage("Scene Saved");
			}

			if (state.ReloadSceneRequested && _sceneState.CurrentModel != "")
			{
				_sceneConfigController.ReloadSceneConfig(_sceneState);
				_sceneState.Modified = false;
				_ui.ShowStatusMessage("Scene Reloaded");
			}

			if (state.ResetSceneRequested && _sceneState.CurrentModel != "")
			{
				_sceneConfigController.ApplySceneConfig(new SceneConfig(), _sceneState);
				_sceneState.Modified = true;
			}

			var input = Input;

			if (input.Keyboard.IsDown(KeyCode.Q) && !_prevInput.Keyboard.IsDown(KeyCode.Q))
			{
				if (_unsavedChangesGate.RequestExit(_sceneState.Modified && _sceneState.CurrentModel != ""))
					_ui.ShowUnsavedChangesPrompt();
				else
					Close();
			}

			bool hovered     = state.SceneViewportHovered;
			Vector2 mousePos = input.Mouse.Position;
			bool mbLeft      = input.Mouse.LeftDown;
			bool mbMiddle    = input.Mouse.MiddleDown;
			bool orbitHeld   = hovered && mbLeft;
			bool panHeld     = hovered && mbMiddle;
			float wheelDelta = hovered ? Input.Mouse.WheelDelta : 0;

			HandleKeybinds(input, _prevInput, hovered);
			_prevInput = input;

			// ---- Dirty tracking ----
			bool dirty = _dirtyTracker.Update(state, _cameraController, _ui.Selection.SelectedVertexIndex, _ui.Selection.SelectedFaceIndex);
			if (_sceneState.CurrentModel != "" && dirty)
				_sceneState.Modified = true;

			_ui.IsSceneModified = _sceneState.Modified;

			if (state.RequestFocusCamera)
				FocusCamera();

			if (state.RequestResetCamera)
				ResetCamera();

			UpdateCameraControls(_camera, mousePos, orbitHeld, panHeld, wheelDelta);
		}

		private void FocusCamera()
		{
			Mesh? mesh = _entity.Mesh;
			if (mesh is not null && _ui.Selection.SelectedVertexIndex.HasValue)
			{
				int idx = _ui.Selection.SelectedVertexIndex.Value;
				float[] pos = mesh.Vertices[idx].Channels["position"];
				Vector3 worldPos = Vector3.Transform(new Vector3(pos[0], pos[1], pos[2]), _entity.Transforms.Matrix);
				_cameraController.FocusOnPoint(worldPos, 1f);
			}
			else if (mesh is not null && _ui.Selection.SelectedFaceIndex.HasValue)
			{
				int f = _ui.Selection.SelectedFaceIndex.Value;
				int i0 = mesh.Indices[f * 3], i1 = mesh.Indices[f * 3 + 1], i2 = mesh.Indices[f * 3 + 2];
				float[] c0 = mesh.Vertices[i0].Channels["position"];
				float[] c1 = mesh.Vertices[i1].Channels["position"];
				float[] c2 = mesh.Vertices[i2].Channels["position"];
				Matrix4x4 model = _entity.Transforms.Matrix;
				Vector3 p0 = Vector3.Transform(new Vector3(c0[0], c0[1], c0[2]), model);
				Vector3 p1 = Vector3.Transform(new Vector3(c1[0], c1[1], c1[2]), model);
				Vector3 p2 = Vector3.Transform(new Vector3(c2[0], c2[1], c2[2]), model);
				Vector3 centroid = (p0 + p1 + p2) / 3f;
				float radius = MathF.Max(Vector3.Distance(centroid, p0), MathF.Max(Vector3.Distance(centroid, p1), Vector3.Distance(centroid, p2)));
				_cameraController.FocusOnPoint(centroid, MathF.Max(radius, 0.01f));
			}
			else
			{
				_cameraController.FocusOnEntity(_entity);
			}
		}

		private void ResetCamera()
		{
			_cameraController.DesiredTarget   = Vector3.Zero;
			_cameraController.DesiredDistance = 10f;
			_cameraController.DesiredYaw      = Mathematics.DegreesToRadians(135f);
			_cameraController.DesiredPitch    = Mathematics.DegreesToRadians(-30f);
		}

		private void HandleKeybinds(InputState input, InputState prev, bool sceneHovered)
		{
			bool sceneActive = sceneHovered || _ui.State.SceneViewportFocused;
			if (!sceneActive)
				return;

			bool Pressed(KeyCode key) => input.Keyboard.IsDown(key) && !prev.Keyboard.IsDown(key);

			if (Pressed(KeyCode.F))    FocusCamera();
			if (Pressed(KeyCode.R))    ResetCamera();
			if (Pressed(KeyCode.Z))    _ui.CycleRenderMode();
			if (Pressed(KeyCode.Tab))  _ui.CycleSelectionMode();
			if (Pressed(KeyCode.N))    _ui.ToggleNormals();
			if (Pressed(KeyCode.B))    _ui.ToggleBackfaceCulling();
			if (Pressed(KeyCode.G))    _ui.ToggleGrid();
		}

		private void RebuildSceneHierarchy()
		{
			var root = new SceneHierarchyItem {
				Id = "scene-root",
				Name = "Main Scene",
				Type = SceneItemType.Root,
				Target = null
			};

			_entityHierarchyItem = new SceneHierarchyItem {
				Id = "entity",
				Name = "Entity",
				Type = SceneItemType.Entity,
				Target = _entity
			};
			root.Children.Add(_entityHierarchyItem);

			var lights = new SceneHierarchyItem {
				Id = "group-lights",
				Name = "Lights",
				Type = SceneItemType.Group
			};

			lights.Children.Add(new SceneHierarchyItem {
				Id = "light-ambient",
				Name = "AmbientLight",
				Type = SceneItemType.Light,
				Target = _lighting.Ambient
			});

			if (_lighting.DirectionalLights.Count > 0)
			{
				for (int i = 0; i < _lighting.DirectionalLights.Count; i++)
				{
					lights.Children.Add(new SceneHierarchyItem {
						Id = $"light-directional-{i}",
						Name = $"DirectionalLight {i}",
						Type = SceneItemType.Light,
						Target = _lighting.DirectionalLights[i]
					});
				}
			}

			var cameras = new SceneHierarchyItem {
				Id = "group-cameras",
				Name = "Cameras",
				Type = SceneItemType.Group
			};

			cameras.Children.Add(new SceneHierarchyItem {
				Id = "camera-main",
				Name = "MainCamera",
				Type = SceneItemType.Camera,
				Target = _camera
			});

			root.Children.Add(lights);
			root.Children.Add(cameras);

			_ui.SetSceneHierarchy(root);
		}

		/// <summary>Applies orbit, pan, and zoom inputs to the camera controller and writes the result to <paramref name="camera"/>.</summary>
		public void UpdateCameraControls(Camera camera, Vector2 mousePosition, bool orbitHeld, bool panHeld, float wheelDelta)
		{
			Vector2 delta = mousePosition - _lastMousePosition;
			_lastMousePosition = mousePosition;

			if (orbitHeld)
				_cameraController.Orbit(delta);

			if (panHeld)
				_cameraController.Pan(delta);

			if (wheelDelta != 0f)
				_cameraController.Zoom(wheelDelta);

			_cameraController.Update(DeltaTime);
			_cameraController.Apply(camera);
		}

		private void ApplyRenderMode(ViewState state)
		{
			switch (state.RenderMode)
			{
				case ViewRenderMode.Solid:
					RenderFlags.PolygonModeFill();
					RenderFlags.DisableProgramPointSize();
					_entity?.Material?.SetParameter("uPointSize", 1.0f);
					_entity?.Material?.SetParameter("uRenderMode", 0);
					break;

				case ViewRenderMode.Wireframe:
					RenderFlags.PolygonModeLine();
					RenderFlags.DisableProgramPointSize();
					_entity?.Material?.SetParameter("uPointSize", 1.0f);
					_entity?.Material?.SetParameter("uRenderMode", 1);
					break;

				case ViewRenderMode.Points:
					RenderFlags.PolygonModePoint();
					RenderFlags.EnableProgramPointSize();
					_entity?.Material?.SetParameter("uPointSize", 2.0f);
					_entity?.Material?.SetParameter("uRenderMode", 2);
					break;
			}
		}

		protected override void OnRender()
		{
			if (_sceneTarget is null)
				return;

			ViewState state = _ui.State;
			Vector2 panelSize = state.SceneViewportSize;
			int sceneWidth = Math.Max(1, (int)panelSize.X);
			int sceneHeight = Math.Max(1, (int)panelSize.Y);

			_sceneTarget.Resize(sceneWidth, sceneHeight);

			Render.BeginTarget(_sceneTarget);
			Render.ClearColor = OxocarbonTheme.Base01;
			Render.BeginFrame();

			if (state.BackfaceCulling)
				RenderFlags.EnableCullFace();
			else
				RenderFlags.DisableCullFace();
			RenderFlags.EnableDepthTest();

			ApplyRenderMode(state);
			bool entityVisible = _entityHierarchyItem?.Visible ?? true;
			if (entityVisible)
			{
				Render.Draw(_entity, _camera, _lighting, (float)sceneWidth / sceneHeight);

				if (state.ShowNormals && _normalsShader is not null)
				{
					RenderFlags.PolygonModeFill();
					RenderFlags.DisableProgramPointSize();
					_normalsOverlay.Draw(_normalsShader, _entity, _camera, (float)sceneWidth / sceneHeight, state.NormalsScale);
				}
			}

			if (state.ShowGrid && _gridShader is not null && _floorGrid is not null)
			{
				RenderFlags.PolygonModeFill();
				RenderFlags.DisableCullFace();
				RenderFlags.EnableBlend();
				_floorGrid.Draw(_gridShader, _camera, (float)sceneWidth / sceneHeight);
				RenderFlags.DisableBlend();
			}

			Render.EndTarget(Width, Height);

			Render.ClearColor = new(0.08f, 0.08f, 0.08f, 1.0f);
			Render.BeginFrame();

			_ui.Render(
				DeltaTime,
				Width,
				Height,
				Input.Mouse.Position,
				Input.Mouse.WheelDelta,
				Input.Mouse.LeftDown,
				Input.Mouse.RightDown,
				Input.Mouse.MiddleDown,
				Input.Keyboard.IsDown(KeyCode.LeftControl),
				Input.Keyboard.IsDown(KeyCode.LeftShift),
				Input.Keyboard.IsDown(KeyCode.LeftAlt),
				Input.Keyboard.IsDown(KeyCode.LeftSuper),
				Input.TextInput
			);

			SwapBuffers();
		}

		private void DrainCommandQueue()
		{
			bool hadApiData = _apiMeshController.HasApiData;

			_apiMeshController.DrainCommandQueue(
				_commandQueue, _entity, _animController, _meshLibrary, _sceneState,
				LoadApiSceneMetadata, () => _sceneConfigController.ApplyApiSceneConfig(_sceneState));

			bool hasApiData = _apiMeshController.HasApiData;
			if (hasApiData != hadApiData)
				_ui.ApiDataMode = hasApiData;
		}

		/// <summary>
		/// Switches the active scene identity to <paramref name="sceneName"/> and loads (or creates)
		/// its <c>.scene</c> sidecar, so API-provided meshes can persist vertex layout, material,
		/// camera, and lighting just like file-backed assets.
		/// </summary>
		private void LoadApiSceneMetadata(string sceneName)
		{
			_sceneState.CurrentModel = sceneName;
			AssetMetadata meta = AssetMetadata.Load(AssetPaths.ModelPath(sceneName));
			_meshLibrary.SetMetadata(sceneName, meta);
		}
	}
}
