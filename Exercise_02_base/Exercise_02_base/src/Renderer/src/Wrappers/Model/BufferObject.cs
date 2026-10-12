using System.Runtime.CompilerServices;
using OpenTK.Graphics.OpenGL;

namespace Renderer.Wrappers.Model
{
	public abstract class BufferObject(BufferTarget target) : GLDisposable
	{
        public int Handle { get; } = GL.GenBuffer();
        public BufferTarget Target { get; } = target;

        public virtual void Bind()
		{
			ThrowIfDisposed();
			GL.BindBuffer(Target, Handle);
		}

		public virtual void Unbind()
		{
			ThrowIfDisposed();
			GL.BindBuffer(Target, 0);
		}

		public void SetData<T>(BufferUsageHint usage, T[] data) where T : unmanaged
		{
			ArgumentNullException.ThrowIfNull(data);
			Bind();
			int sizeInBytes = data.Length * Unsafe.SizeOf<T>();
			GL.BufferData(Target, sizeInBytes, data, usage);
		}

		protected override void GLDispose()
		{
			GL.DeleteBuffer(Handle);
		}
	}
}
