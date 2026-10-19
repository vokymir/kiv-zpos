using OpenTK.Graphics.OpenGL;

namespace Renderer.Wrappers.Shaders
{
	public sealed class VertexShader(string strPath) : Shader(ShaderType.VertexShader, strPath) { }
	public sealed class FragmentShader(string strPath) : Shader(ShaderType.FragmentShader, strPath) { }
	public sealed class ComputeShader(string strPath) : Shader(ShaderType.ComputeShader, strPath) { }
}
