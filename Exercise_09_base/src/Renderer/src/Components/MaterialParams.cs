using System.Numerics;

namespace Renderer.Components
{
	/// <summary>Shared shader uniform names and default values for the standard material parameter set
	/// (tint, specular color/strength, shininess), used by <c>Render</c>, <c>EntityDrawer</c>, and
	/// <c>SceneConfigController</c>.</summary>
	public static class MaterialParams
	{
		public const string Tint = "uTint";
		public const string SpecularColor = "uSpecularColor";
		public const string SpecularStrength = "uSpecularStrength";
		public const string Shininess = "uShininess";

		public static readonly Vector3 DefaultTint = new(0.9f, 0.9f, 0.9f);
		public static readonly Vector3 DefaultSpecularColor = Vector3.One;
		public const float DefaultSpecularStrength = 1f;
		public const float DefaultShininess = 32f;
	}
}
