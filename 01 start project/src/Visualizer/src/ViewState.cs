using Renderer.Abstractions;
using System.Numerics;
using Visualizer.Modals;

namespace Visualizer
{
	/// <summary>
	/// Immutable snapshot of all UI-driven view settings and one-shot frame events.
	/// Published by <see cref="UiController"/> at the end of each rendered frame so
	/// <see cref="AppWindow"/> can read it once per <c>OnUpdate</c> without polling
	/// individual properties.
	/// </summary>
	public sealed record ViewState
	{
		/* ---- Viewport ---- */
		public Vector2 SceneViewportSize    { get; init; }
		public bool    SceneViewportHovered { get; init; }
		public bool    SceneViewportFocused { get; init; }

		/* ---- Render settings ---- */
		public ViewRenderMode RenderMode      { get; init; } = ViewRenderMode.Solid;
		public SelectionMode  SelectionMode   { get; init; } = SelectionMode.Vertex;
		public bool           BackfaceCulling { get; init; } = true;
		public bool           ShowNormals     { get; init; }
		public bool           ShowGrid        { get; init; } = true;
		public float          NormalsScale    { get; init; } = 1.0f;

		/* ---- Model / shader / animation ---- */
		public bool    ModelChangedThisFrame               { get; init; }
		public bool    ReloadModelRequested                { get; init; }
		public string? SelectedModel                       { get; init; }
		public bool    ShaderChangedThisFrame              { get; init; }
		public bool    ReloadShaderRequested               { get; init; }
		public string? SelectedShader                      { get; init; } = "default";
		public bool    AnimationChangedThisFrame           { get; init; }
		public string? SelectedAnimation                   { get; init; }

		/* ---- Asset / layout ---- */
		public bool LayoutChangedThisFrame                 { get; init; }
		public bool GenerateNormalsMetaChangedThisFrame    { get; init; }

		/* ---- Camera requests ---- */
		public bool RequestResetCamera { get; init; }
		public bool RequestFocusCamera { get; init; }

		/* ---- Scene config ---- */
		public bool SaveSceneRequested    { get; init; }
		public bool ReloadSceneRequested  { get; init; }
		public bool ResetSceneRequested   { get; init; }

		/* ---- Unsaved changes modal ---- */
		public UnsavedChangesResult UnsavedChangesResult { get; init; }
	}
}
