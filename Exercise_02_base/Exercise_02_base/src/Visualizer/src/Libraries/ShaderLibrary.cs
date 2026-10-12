using Renderer.Wrappers.Shaders;

namespace Visualizer
{
	/// <summary>Cache of loaded <see cref="ShaderProgram"/> objects keyed by shader name. Loads on first access.</summary>
	public sealed class ShaderLibrary : IDisposable
	{
		private readonly Dictionary<string, ShaderProgram> _programs = [];

		/// <summary>Returns the compiled and linked program for <paramref name="name"/>, loading it on first access.</summary>
		/// <param name="name">Base name of the shader pair, e.g. "default" for default.vert.glsl / default.frag.glsl.</param>
		public ShaderProgram Get(string name)
		{
			if (_programs.TryGetValue(name, out ShaderProgram? program))
				return program;

			program = CreateProgram(name);
			_programs.Add(name, program);
			return program;
		}

		/// <summary>Recompiles <paramref name="name"/> from disk and replaces the cached program, disposing the old one.</summary>
		public void Reload(string name)
		{
			ShaderProgram program = CreateProgram(name);
			if (_programs.TryGetValue(name, out ShaderProgram? old))
				old.Dispose();
			_programs[name] = program;
		}

		private static ShaderProgram CreateProgram(string name)
		{
			string dir = AssetPaths.ShadersDir;

			VertexShader vert = new(Path.Combine(dir, $"{name}.vert.glsl"));
			FragmentShader frag = new(Path.Combine(dir, $"{name}.frag.glsl"));

			vert.Compile();
			frag.Compile();

			ShaderProgram program = new(vert, frag);
			program.Link();

			vert.Dispose();
			frag.Dispose();

			return program;
		}

		public void Dispose()
		{
			foreach (string key in _programs.Keys.ToList())
			{
				_programs[key].Dispose();
				_programs.Remove(key);
			}
		}
	}
}
