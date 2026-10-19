using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Renderer
{
	public abstract class Context : IBindingsContext
	{
		public static void LoadBindings(IBindingsContext context) => GL.LoadBindings(context);
		public abstract IntPtr GetProcAddress(string procName);
	}
}
