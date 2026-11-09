using OpenTK.Graphics.OpenGL;

namespace Renderer.Wrappers.Textures
{
	public sealed class Texture2D : GLDisposable
	{
		public int Handle { get; }
		public int Width  { get; private set; }
		public int Height { get; private set; }

		public Texture2D(int width, int height)
		{
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

            Width = width;
			Height = height;

			Handle = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, Handle);

			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

			GL.TexImage2D(
				TextureTarget.Texture2D,
				0,
				PixelInternalFormat.Rgba,
				width,
				height,
				0,
				PixelFormat.Rgba,
				PixelType.UnsignedByte,
				IntPtr.Zero
			);

			GL.BindTexture(TextureTarget.Texture2D, 0);
		}

		public Texture2D(int width, int height, ReadOnlySpan<byte> rgba8)
		{
			Width = width;
			Height = height;

			Handle = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, Handle);

			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

			GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
			GL.TexImage2D(
				TextureTarget.Texture2D,
				0,
				PixelInternalFormat.Rgba,
				width,
				height,
				0,
				PixelFormat.Rgba,
				PixelType.UnsignedByte,
				rgba8.ToArray()
			);

			GL.BindTexture(TextureTarget.Texture2D, 0);
		}

		public void Bind(int unit = 0)
		{
			GL.ActiveTexture(TextureUnit.Texture0 + unit);
			GL.BindTexture(TextureTarget.Texture2D, Handle);
		}

		public void Resize(int width, int height)
		{
			if (width <= 0 || height <= 0)
				return;

			if (Width == width && Height == height)
				return;

			Width = width;
			Height = height;

			GL.BindTexture(TextureTarget.Texture2D, Handle);
			GL.TexImage2D(
				TextureTarget.Texture2D,
				0,
				PixelInternalFormat.Rgba,
				width,
				height,
				0,
				PixelFormat.Rgba,
				PixelType.UnsignedByte,
				IntPtr.Zero
			);
			GL.BindTexture(TextureTarget.Texture2D, 0);
		}

		protected override void GLDispose()
		{
			GL.DeleteTexture(Handle);
		}
	}
}
