using OpenTK.Graphics.OpenGL4;

namespace Renderer.Wrappers.Textures
{
	public sealed class RenderTarget : GLDisposable
	{
		public int Handle { get; private set; }
		public Texture2D ColorTexture { get; private set; }
		public int DepthRenderbuffer { get; private set; }

		public int Width { get; private set; }
		public int Height { get; private set; }

		public RenderTarget(int width, int height)
		{
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

            Width = width;
			Height = height;

			Handle = GL.GenFramebuffer();
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, Handle);

			ColorTexture = new Texture2D(width, height);
			GL.FramebufferTexture2D(
				FramebufferTarget.Framebuffer,
				FramebufferAttachment.ColorAttachment0,
				TextureTarget.Texture2D,
				ColorTexture.Handle,
				0
			);

			DepthRenderbuffer = GL.GenRenderbuffer();
			GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, DepthRenderbuffer);
			GL.RenderbufferStorage(
					RenderbufferTarget.Renderbuffer,
					RenderbufferStorage.Depth24Stencil8,
					width,
					height);

			GL.FramebufferRenderbuffer(
					FramebufferTarget.Framebuffer,
					FramebufferAttachment.DepthStencilAttachment,
					RenderbufferTarget.Renderbuffer,
					DepthRenderbuffer);

			FramebufferErrorCode status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
			if (status != FramebufferErrorCode.FramebufferComplete)
				throw new InvalidOperationException($"Framebuffer is incomplete: {status}");

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
		}

		public void Bind()
		{
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, Handle);
		}

		public static void Unbind()
		{
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
		}

		public void Resize(int width, int height)
		{
			if (width <= 0 || height <= 0)
				return;

			if (Width == width && Height == height)
				return;

			Width = width;
			Height = height;

			ColorTexture.Resize(width, height);

			GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, DepthRenderbuffer);
			GL.RenderbufferStorage(
					RenderbufferTarget.Renderbuffer,
					RenderbufferStorage.Depth24Stencil8,
					width,
					height);
			GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
		}

		protected override void GLDispose()
		{
			ColorTexture.Dispose();

			if (DepthRenderbuffer != 0)
				GL.DeleteRenderbuffer(DepthRenderbuffer);

			if (Handle != 0)
				GL.DeleteFramebuffer(Handle);
		}
	}
}
