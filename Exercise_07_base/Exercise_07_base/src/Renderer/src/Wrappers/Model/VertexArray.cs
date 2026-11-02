using OpenTK.Graphics.OpenGL;

namespace Renderer.Wrappers.Model
{
    public sealed class VertexArray : GLDisposable
    {
		public int Handle { get; }

		public VertexBuffer? VertexBuffer { get; private set; }
		public ElementBuffer? ElementBuffer { get; private set; }

		private readonly HashSet<int> _enabledAttributes = [];

		public VertexArray()
		{
			Handle = GL.GenVertexArray();
		}

		public bool Bind()
		{
			ThrowIfDisposed();
			GL.BindVertexArray(Handle);
			return true;
		}

		public bool Unbind()
		{
			ThrowIfDisposed();
			GL.BindVertexArray(0);
			return true;
		}

		public void SetVertexBuffer(VertexBuffer buffer)
		{
			ArgumentNullException.ThrowIfNull(buffer);
			ThrowIfDisposed();
			Bind();
			buffer.Bind();
			VertexBuffer = buffer;
		}

		public void SetElementBuffer(ElementBuffer buffer)
		{
			ArgumentNullException.ThrowIfNull(buffer);
			ThrowIfDisposed();
			Bind();
			buffer.Bind();
			ElementBuffer = buffer;
		}

		public void SetVertexData<T>(BufferUsageHint usage, T[] data) where T : unmanaged
		{
			ThrowIfDisposed();
			if (VertexBuffer is null) throw new InvalidOperationException("No vertex buffer is attached to this vertex array.");
			Bind();
			VertexBuffer.SetData(usage, data);
		}

		public void SetElementData<T>(BufferUsageHint usage, T[] data) where T : unmanaged
		{
			ThrowIfDisposed();
			if (ElementBuffer is null) throw new InvalidOperationException("No element buffer is attached to this vertex array.");
			Bind();
			ElementBuffer.SetData(usage, data);
		}

		public void ApplyLayout(VertexLayout layout)
		{
			ArgumentNullException.ThrowIfNull(layout);
			ThrowIfDisposed();
			if (VertexBuffer is null) throw new InvalidOperationException("Cannot define vertex attributes without a vertex buffer attached.");

			Bind();
			VertexBuffer.Bind();

			foreach (var attribute in layout.Attributes)
			{
				GL.VertexAttribPointer(
					attribute.Index,
					attribute.ComponentCount,
					attribute.Type,
					attribute.Normalized,
					attribute.StrideBytes,
					attribute.OffsetBytes
				);
				if (_enabledAttributes.Add(attribute.Index)) GL.EnableVertexAttribArray(attribute.Index);
			}
		}

		public void DisableAttribute(int index)
		{
			ThrowIfDisposed();
			Bind();
			if (_enabledAttributes.Remove(index)) GL.DisableVertexAttribArray(index);
		}

        protected override void GLDispose()
        {
			GL.DeleteVertexArray(Handle);
        }
    }
}
