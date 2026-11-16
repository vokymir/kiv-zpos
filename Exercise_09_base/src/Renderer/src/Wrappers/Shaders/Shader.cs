using OpenTK.Graphics.OpenGL;

namespace Renderer.Wrappers.Shaders
{
    public abstract class Shader(ShaderType type, string srcPath) : GLDisposable
    {
        public int Handle { get; } = GL.CreateShader(type);
        public ShaderType Type { get; } = type;
        public string SrcPath { get; } = srcPath;
        public bool   IsCompiled { get; private set; } = false;

        public void Compile()
		{
			ThrowIfDisposed();

            string source = File.ReadAllText(SrcPath);

            GL.ShaderSource(Handle, source);
            GL.CompileShader(Handle);

            GL.GetShader(Handle, ShaderParameter.CompileStatus, out int code);
            if (code == (int)All.True) {
				IsCompiled = true;
				return;
			}

			string typeStr = Type.ToString();
			string message = GL.GetShaderInfoLog(Handle);
			throw new Exception($"Error compiling Shader: {typeStr}: {message}");
		}

        protected override void GLDispose()
        {
			GL.DeleteShader(Handle);
        }
    }
}
