using OpenTK.Graphics.OpenGL;

namespace Renderer
{
	/// <summary>Entry points for querying the underlying graphics API without exposing GL types to consumers.</summary>
	public static class API
	{
		/// <summary>Returns the graphics driver/version string reported by the backend.</summary>
		public static string GetVersionString() => GL.GetString(StringName.Version);

		/// <summary>Returns the name of the graphics API.</summary>
		public static string GetAPIName() => "OpenGL";
	}
}
