namespace Visualizer
{
	/// <summary>Centralizes the layout of the <c>Assets/</c> directory shipped alongside the app.</summary>
	public static class AssetPaths
	{
		public static string ModelsDir  => Path.Combine(AppContext.BaseDirectory, "Assets", "Models");
		public static string ShadersDir => Path.Combine(AppContext.BaseDirectory, "Assets", "Shaders");
		public static string FontsDir   => Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts");

		/// <summary>Resolves <paramref name="kind"/> against <see cref="ModelsDir"/>, unless it's already an absolute path.</summary>
		public static string ModelPath(string kind) =>
			Path.IsPathRooted(kind) ? kind : Path.Combine(ModelsDir, kind);
	}
}
