namespace Renderer
{
	/// <summary>Base class for objects that own OpenGL resources and must be explicitly disposed.</summary>
	public abstract class GLDisposable : IDisposable
	{
		private bool _disposed = false;

		/// <summary>Override to release OpenGL resources. Called exactly once by <see cref="Dispose"/>.</summary>
		protected abstract void GLDispose();

		/// <summary>Throws <see cref="ObjectDisposedException"/> if this object has already been disposed.</summary>
		protected void ThrowIfDisposed()
		{
			ObjectDisposedException.ThrowIf(_disposed, this);
		}

		/// <summary>Releases OpenGL resources. Safe to call multiple times.</summary>
		public void Dispose()
		{
			if (_disposed) return;
			GLDispose();
			_disposed = true;
			GC.SuppressFinalize(this);
		}
	}
}
