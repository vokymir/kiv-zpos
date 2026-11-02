using Core.PostProcessing;
using Renderer.Wrappers.Model;

namespace Visualizer
{
	/// <summary>
	/// Full scene state recorded alongside a model asset.
	/// Stored as the <c>scene</c> field in the .scene JSON sidecar.
	/// All fields are nullable so that a partial config only overrides what was explicitly saved.
	/// </summary>
	public sealed class SceneConfig
	{
		public SceneModelConfig?     Model      { get; set; }
		public SceneCameraConfig?    Camera     { get; set; }
		public SceneLightingConfig?  Lighting   { get; set; }
		public SceneSelectionConfig? Selection  { get; set; }
		public SceneViewConfig?      View       { get; set; }
		public SceneTransformsConfig? Transforms { get; set; }
		public SceneAnimationConfig?  Animation  { get; set; }
	}

	public sealed class SceneModelConfig
	{
		public string?  DisplayName  { get; set; }
		public string?  Format       { get; set; }
		public float    ImportScale  { get; set; } = 1f;
		public NormalsMode NormalsMode { get; set; } = NormalsMode.Flat;
		public Dictionary<string, string> Tags { get; set; } = [];
		public List<VertexChannelSpec>? VertexLayout { get; set; }
		public SceneMaterialConfig? Material { get; set; }
	}

	public sealed class SceneMaterialConfig
	{
		/// <summary>RGB tint as [r, g, b] floats.</summary>
		public float[]? Tint            { get; set; }
		/// <summary>RGB specular color as [r, g, b] floats.</summary>
		public float[]? SpecularColor   { get; set; }
		public float?   SpecularStrength { get; set; }
		public float?   Shininess        { get; set; }
	}

	public sealed class SceneTransformsConfig
	{
		/// <summary>[x, y, z]</summary>
		public float[]? Position { get; set; }
		/// <summary>[x, y, z, w] quaternion.</summary>
		public float[]? Rotation { get; set; }
		/// <summary>[x, y, z]</summary>
		public float[]? Scale    { get; set; }
	}

	/// <summary>Orbit camera state. Stored as the controller's desired values, not derived position/rotation.</summary>
	public sealed class SceneCameraConfig
	{
		/// <summary>Orbit target point as [x, y, z].</summary>
		public float[]? Target   { get; set; }
		public float    Distance { get; set; } = 10f;
		/// <summary>Horizontal angle in radians.</summary>
		public float    Yaw      { get; set; }
		/// <summary>Vertical angle in radians.</summary>
		public float    Pitch    { get; set; }
	}

	public sealed class SceneLightingConfig
	{
		public SceneAmbientConfig?              Ambient          { get; set; }
		public List<SceneDirectionalLightConfig>? DirectionalLights { get; set; }
	}

	public sealed class SceneAmbientConfig
	{
		/// <summary>RGB as [r, g, b].</summary>
		public float[]? Color     { get; set; }
		public float    Intensity { get; set; } = 0.1f;
	}

	public sealed class SceneDirectionalLightConfig
	{
		/// <summary>RGB as [r, g, b].</summary>
		public float[]? Color     { get; set; }
		public float    Intensity { get; set; } = 1f;
		/// <summary>Direction vector as [x, y, z] (normalised on apply).</summary>
		public float[]? Direction { get; set; }
	}

	public sealed class SceneSelectionConfig
	{
		public int?    VertexIndex { get; set; }
		public int?    FaceIndex   { get; set; }
		/// <summary>"Vertex" or "Face".</summary>
		public string? Mode        { get; set; }
	}

	public sealed class SceneAnimationConfig
	{
		public string? SequenceName { get; set; }
		public int     FrameIndex   { get; set; }
		public float   FPS         { get; set; } = 24f;
		public float   Speed       { get; set; } = 1f;
		public bool    IsReverse   { get; set; }
		public bool    IsPlaying   { get; set; }
	}

	public sealed class SceneViewConfig
	{
		/// <summary>"Solid", "Wireframe", or "Points".</summary>
		public string? RenderMode     { get; set; }
		public string? Shader         { get; set; }
		public bool    ShowNormals    { get; set; }
		public float   NormalsScale   { get; set; } = 1f;
		public bool    BackfaceCulling { get; set; } = true;
	}
}
