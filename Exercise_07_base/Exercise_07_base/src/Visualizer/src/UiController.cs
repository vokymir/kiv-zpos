using Renderer.Abstractions;
using Renderer.Components;
using Renderer.Objects;
using Renderer.Wrappers.Textures;
using System.Numerics;
using Visualizer.GUI;
using Visualizer.Modals;
using Visualizer.Scripts;
using Visualizer.UI;

namespace Visualizer
{
	public enum SelectionMode { Vertex, Face }

	/// <summary>
	/// Coordinates all ImGui panels: dockspace, toolbar, hierarchy, inspector, scene viewport, and animation.
	/// Runs one frame per <see cref="Render"/> call and exposes aggregated state via <see cref="State"/>.
	/// </summary>
	public sealed class UiController : IDisposable
	{
		private ImGuiFrontend? _imgui;
		private bool _initialized;

		/* ---- Panels ---- */
		private readonly ToolbarPanel       _toolbar   = new();
		private readonly HierarchyPanel     _hierarchy = new();
		private readonly InspectorPanel     _inspector = new();
		private readonly SceneViewportPanel _viewport  = new();
		private readonly AnimationPanel     _animation = new();
		private readonly StatusBarPanel     _statusBar = new();

		/* ---- Unsaved changes modal (opened externally) ---- */
		private readonly UnsavedChangesModal _unsavedModal = new();

		/* ---- Scene context ---- */
		private Camera? _sceneCamera;
		private Entity? _sceneEntity;

		private readonly SelectionController _selection = new();
		internal SelectionController Selection => _selection;

		private UiTextureHandle _sceneTexture;
		private MeshAnimationController? _animController;

		private Vector2 _mousePos;
		private bool    _mouseRightClicked;
		private bool    _prevMouseRight;
		private object?       _lastSelectionContext;
		private SelectionMode _lastSelectionMode;

		/* ---- Aggregated state snapshot ---- */
		public ViewState State { get; private set; } = new();

		/* ---- Forwarded toolbar properties ---- */
		public Vector2 SceneViewportSize => _viewport.Size;

		public string? CustomModelPath
		{
			get => _toolbar.CustomModelPath;
			set => _toolbar.CustomModelPath = value;
		}

		public bool IsSceneModified
		{
			get => _toolbar.IsSceneModified;
			set => _toolbar.IsSceneModified = value;
		}

		public object? SelectedObject => _hierarchy.SelectedObject;

		public Action? OnAddDirectionalLight
		{
			get => _hierarchy.OnAddDirectionalLight;
			set => _hierarchy.OnAddDirectionalLight = value;
		}

		public Action<object>? OnRemoveDirectionalLight
		{
			get => _hierarchy.OnRemoveDirectionalLight;
			set => _hierarchy.OnRemoveDirectionalLight = value;
		}

		/* ---- Initialization ---- */

		public void Initialize(ImGuiFrontend frontend)
		{
			_imgui = frontend ?? throw new ArgumentNullException(nameof(frontend));
			_initialized = true;
		}

		public void Dispose() => _imgui?.Dispose();

		/* ---- Setup ---- */

		public UiTextureHandle RegisterTexture(Texture2D texture)
		{
			ThrowIfUninitialized();
			return _imgui!.RegisterTexture(texture);
		}

		public void SetSceneTexture(UiTextureHandle texture)       => _sceneTexture  = texture;
		public void SetSceneHierarchy(SceneHierarchyItem root)     => _hierarchy.SetRoot(root ?? throw new ArgumentNullException(nameof(root)));
		public void SetAnimationController(MeshAnimationController c) => _animController = c;

		public void SetSceneContext(Camera camera, Entity entity)
		{
			_sceneCamera = camera;
			_sceneEntity = entity;
		}

		public void SetLayoutDataProvider(Func<(AssetMetadata, string, string[])?>? provider)
			=> _toolbar.LayoutDataProvider = provider;

		public void SetLoadingProvider(Func<bool>? provider)
			=> _statusBar.IsLoadingProvider = provider;

		public bool ApiDataMode
		{
			set { _toolbar.ApiDataMode = value; _animation.ApiDataMode = value; }
		}

		/* ---- Status message ---- */

		public void ShowStatusMessage(string message) => _statusBar.ShowMessage(message);

		/* ---- Commands ---- */

		public void TriggerFocusCamera()    => _toolbar.RequestFocusCamera  = true; // direct set bypasses private setter
		public void TriggerResetCamera()    => _toolbar.RequestResetCamera   = true;
		public void ToggleNormals()         => _toolbar.ShowNormals          = !_toolbar.ShowNormals;
		public void ToggleBackfaceCulling() => _toolbar.BackfaceCulling      = !_toolbar.BackfaceCulling;
		public void ToggleGrid()            => _toolbar.ShowGrid             = !_toolbar.ShowGrid;
		public void SetRenderMode(ViewRenderMode mode) => _toolbar.RenderMode = mode;

		public void CycleRenderMode()
		{
			_toolbar.RenderMode = _toolbar.RenderMode switch
			{
				ViewRenderMode.Solid     => ViewRenderMode.Wireframe,
				ViewRenderMode.Wireframe => ViewRenderMode.Points,
				_                        => ViewRenderMode.Solid,
			};
		}

		public void SetSelectionMode(SelectionMode mode)
		{
			if (mode == _toolbar.SelectionMode) return;
			_toolbar.SelectionMode = mode;
			_lastSelectionMode = mode;
			_selection.ResetSelection();
		}

		public void CycleSelectionMode() =>
			SetSelectionMode(_toolbar.SelectionMode == SelectionMode.Vertex ? SelectionMode.Face : SelectionMode.Vertex);

		public void SetNormals(bool show, float scale)
		{
			_toolbar.ShowNormals  = show;
			_toolbar.NormalsScale = scale;
		}

		public void SetBackfaceCulling(bool enabled) => _toolbar.BackfaceCulling = enabled;

		public void RestoreAnimation(string? sequenceName) => _animation.SelectedAnimation = sequenceName;

		public void RevertModel(string modelName) => _toolbar.SelectedModel = modelName;

		public void ShowUnsavedChangesPrompt() => _unsavedModal.Open();

		public void RestoreSelection(int? vertexIndex, int? faceIndex, SelectionMode mode)
		{
			_toolbar.SelectionMode = mode;
			_lastSelectionMode = mode;
			_selection.Restore(vertexIndex, faceIndex);
			_lastSelectionContext = GetSelectionContext();
		}

		/* ---- Render ---- */

		public void Render(
			float deltaTime,
			int width,
			int height,
			Vector2 mousePos,
			float mouseWheel,
			bool mouseLeft,
			bool mouseRight,
			bool mouseMiddle,
			bool keyCtrl,
			bool keyShift,
			bool keyAlt,
			bool keySuper,
			string textInput)
		{
			ThrowIfUninitialized();

			_toolbar.ResetFrameFlags();
			_inspector.ResetFrameFlags();
			_animation.ResetFrameFlags();

			_mousePos = mousePos;
			_mouseRightClicked = mouseRight && !_prevMouseRight;
			_prevMouseRight = mouseRight;

			ImGuiFrameInput input = new()
			{
				DisplaySize      = new Vector2(width, height),
				FramebufferScale = Vector2.One,
				DeltaTime        = deltaTime,
				MousePosition    = mousePos,
				MouseWheel       = mouseWheel,
				MouseWheelH      = 0f,
				MouseLeft        = mouseLeft,
				MouseRight       = mouseRight,
				MouseMiddle      = mouseMiddle,
				KeyCtrl          = keyCtrl,
				KeyShift         = keyShift,
				KeyAlt           = keyAlt,
				KeySuper         = keySuper,
				TextInput        = textInput
			};

			_imgui!.BeginFrame(input);
			BuildUi(deltaTime);
			_imgui.EndFrame();

			State = new ViewState
			{
				SceneViewportSize                   = _viewport.Size,
				SceneViewportHovered                = _viewport.Hovered,
				SceneViewportFocused                = _viewport.Focused,
				RenderMode                          = _toolbar.RenderMode,
				SelectionMode                       = _toolbar.SelectionMode,
				BackfaceCulling                     = _toolbar.BackfaceCulling,
				ShowNormals                         = _toolbar.ShowNormals,
				NormalsScale                        = _toolbar.NormalsScale,
				ShowGrid                            = _toolbar.ShowGrid,
				ModelChangedThisFrame               = _toolbar.ModelChangedThisFrame,
				ReloadModelRequested                = _toolbar.ReloadModelRequested,
				SelectedModel                       = _toolbar.SelectedModel,
				ShaderChangedThisFrame              = _toolbar.ShaderChangedThisFrame,
				ReloadShaderRequested               = _toolbar.ReloadShaderRequested,
				SelectedShader                      = _toolbar.SelectedShader,
				AnimationChangedThisFrame           = _animation.AnimationChangedThisFrame,
				SelectedAnimation                   = _animation.SelectedAnimation,
				LayoutChangedThisFrame              = _toolbar.LayoutChangedThisFrame,
				GenerateNormalsMetaChangedThisFrame = _inspector.GenerateNormalsMetaChangedThisFrame,
				RequestResetCamera                  = _toolbar.RequestResetCamera,
				RequestFocusCamera                  = _toolbar.RequestFocusCamera,
				SaveSceneRequested                  = _toolbar.SaveSceneRequested,
				ReloadSceneRequested                = _toolbar.ReloadSceneRequested,
				ResetSceneRequested                 = _toolbar.ResetSceneRequested,
				UnsavedChangesResult                = _unsavedModal.ResultThisFrame,
			};
		}

		private void BuildUi(float deltaTime)
		{
			// Reset selection when the underlying model/sequence is swapped out.
			// While an animation is playing, the entity's mesh reference changes every
			// frame even though it's the same logical model, so selection should persist.
			object? selectionContext = GetSelectionContext();
			if (!Equals(selectionContext, _lastSelectionContext))
			{
				_selection.ResetSelection();
				_lastSelectionContext = selectionContext;
			}
			else
			{
				_selection.ClampToMesh(_sceneEntity?.Mesh);
			}

			// Reserve space at the bottom of the work area before the dockspace is laid out,
			// so docked panels don't extend underneath the status bar.
			_statusBar.Draw(deltaTime);
			DockSpacePanel.Draw();
			_toolbar.Draw(deltaTime);

			if (_toolbar.SelectionMode != _lastSelectionMode)
			{
				_selection.ResetSelection();
				_lastSelectionMode = _toolbar.SelectionMode;
			}

			_hierarchy.Draw();
			_inspector.Draw(_hierarchy.SelectedObject, _selection, _toolbar.LayoutDataProvider);
			_viewport.Draw(_sceneTexture, _selection, _toolbar.SelectionMode, _mousePos, _mouseRightClicked, _sceneCamera, _sceneEntity);
			_animation.Draw(_animController);
			_unsavedModal.Draw();
		}

		/// <summary>
		/// Identity used to detect when the selected mesh/model changes.
		/// While playing an animation, this stays stable across the per-frame mesh
		/// swaps, and across re-uploads of the same scene's animation frames (e.g. live
		/// API updates), so the current selection is preserved.
		/// </summary>
		private object? GetSelectionContext() =>
			_animController is { HasAnimation: true } ? (CustomModelPath, true) : _sceneEntity?.Mesh;

		private void ThrowIfUninitialized()
		{
			if (!_initialized || _imgui is null)
				throw new InvalidOperationException("UI not initialized.");
		}
	}
}
