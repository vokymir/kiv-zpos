using System.Numerics;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using Renderer.Abstractions;
using Renderer.Wrappers.Textures;
using Buffer = System.Buffer;

namespace Connector
{
	public sealed class ImGuiOpenGLBackend() : IImGuiBackend
	{
		private const string VertexShaderSource = """
#version 330 core

layout (location = 0) in vec2 aPosition;
layout (location = 1) in vec2 aUv;
layout (location = 2) in vec4 aColor;

uniform mat4 uProjection;

out vec2 vUv;
out vec4 vColor;

void main()
{
	vUv = aUv;
	vColor = aColor;
	gl_Position = uProjection * vec4(aPosition, 0.0, 1.0);
}
""";

		private const string FragmentShaderSource = """
#version 330 core

in vec2 vUv;
in vec4 vColor;

uniform sampler2D uTexture;

out vec4 FragColor;

void main()
{
	FragColor = vColor * texture(uTexture, vUv);
}
""";

		private readonly ImGuiTextureRegistry _textures = new();

		private int _shaderProgram;
		private int _vertexShader;
		private int _fragmentShader;

		private int _vao;
		private int _vbo;
		private int _ebo;

		private int _vertexBufferSize;
		private int _indexBufferSize;

		private int _uProjectionLocation;
		private int _uTextureLocation;

		private Texture2D? _fontTexture;
		private UiTextureHandle _fontTextureHandle;

		private bool _frameBegun;
		private bool _disposed;

		private bool _initialized;

		public void Initialize()
		{
			if (_initialized) return;
			CreateDeviceResources();
			CreateFontTexture();
			SetupImGuiKeyMappings();
			_initialized = true;
		}

		public UiTextureHandle RegisterTexture(Texture2D texture)
		{
			ThrowIfDisposed();
			return _textures.Register(texture);
		}

		public bool UnregisterTexture(UiTextureHandle handle)
		{
			ThrowIfDisposed();
			return _textures.Unregister(handle);
		}

		public void BeginFrame(ImGuiFrameInput input)
		{
			ThrowIfDisposed();

			if (_frameBegun)
				throw new InvalidOperationException("ImGui frame already begun.");

			ImGuiIOPtr io = ImGui.GetIO();

			io.DisplaySize = input.DisplaySize;
			io.DisplayFramebufferScale = input.FramebufferScale;
			io.DeltaTime = input.DeltaTime > 0 ? input.DeltaTime : 1f / 60f;

			io.MousePos = input.MousePosition;
			io.MouseDown[0] = input.MouseLeft;
			io.MouseDown[1] = input.MouseRight;
			io.MouseDown[2] = input.MouseMiddle;
			io.MouseWheel = input.MouseWheel;
			io.MouseWheelH = input.MouseWheelH;

			io.KeyCtrl = input.KeyCtrl;
			io.KeyShift = input.KeyShift;
			io.KeyAlt = input.KeyAlt;
			io.KeySuper = input.KeySuper;

			if (!string.IsNullOrEmpty(input.TextInput))
			{
				foreach (char c in input.TextInput)
					io.AddInputCharacter(c);
			}

			ImGui.NewFrame();
			_frameBegun = true;
		}

		public void EndFrame()
		{
			ThrowIfDisposed();

			if (!_frameBegun)
				throw new InvalidOperationException("ImGui frame has not begun.");

			_frameBegun = false;

			ImGui.Render();
			RenderDrawData(ImGui.GetDrawData());
		}

		private void CreateDeviceResources()
		{
			_vertexShader = GL.CreateShader(ShaderType.VertexShader);
			GL.ShaderSource(_vertexShader, VertexShaderSource);
			GL.CompileShader(_vertexShader);
			CheckShaderCompile(_vertexShader, "ImGui vertex shader");

			_fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
			GL.ShaderSource(_fragmentShader, FragmentShaderSource);
			GL.CompileShader(_fragmentShader);
			CheckShaderCompile(_fragmentShader, "ImGui fragment shader");

			_shaderProgram = GL.CreateProgram();
			GL.AttachShader(_shaderProgram, _vertexShader);
			GL.AttachShader(_shaderProgram, _fragmentShader);
			GL.LinkProgram(_shaderProgram);
			CheckProgramLink(_shaderProgram, "ImGui shader program");

			_uProjectionLocation = GL.GetUniformLocation(_shaderProgram, "uProjection");
			_uTextureLocation = GL.GetUniformLocation(_shaderProgram, "uTexture");

			_vao = GL.GenVertexArray();
			_vbo = GL.GenBuffer();
			_ebo = GL.GenBuffer();

			GL.BindVertexArray(_vao);
			GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);

			int stride = 20; // ImDrawVert = vec2 pos + vec2 uv + u32 color
			GL.EnableVertexAttribArray(0);
			GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);

			GL.EnableVertexAttribArray(1);
			GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 8);

			GL.EnableVertexAttribArray(2);
			GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, stride, 16);

			GL.BindVertexArray(0);
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

			_vertexBufferSize = 10000;
			_indexBufferSize = 2000;

			GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
			GL.BufferData(BufferTarget.ArrayBuffer, _vertexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

			GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
			GL.BufferData(BufferTarget.ElementArrayBuffer, _indexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
		}

		private unsafe void CreateFontTexture()
		{
			ImGuiIOPtr io = ImGui.GetIO();

			io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out int width, out int height, out _);

			int byteCount = width * height * 4;
			byte[] managedPixels = new byte[byteCount];
			fixed (byte* dst = managedPixels)
			{
				Buffer.MemoryCopy(pixels, dst, byteCount, byteCount);
			}

			_fontTexture = new Texture2D(width, height, managedPixels);
			_fontTextureHandle = _textures.Register(_fontTexture);

			io.Fonts.SetTexID(_fontTextureHandle.Id);
			io.Fonts.ClearTexData();
		}

		private static void SetupImGuiKeyMappings()
		{
			ImGuiIOPtr io = ImGui.GetIO();
			io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
		}

		private unsafe void RenderDrawData(ImDrawDataPtr drawData)
		{
			if (drawData.CmdListsCount == 0)
				return;

			int fbWidth = (int)(drawData.DisplaySize.X * drawData.FramebufferScale.X);
			int fbHeight = (int)(drawData.DisplaySize.Y * drawData.FramebufferScale.Y);
			if (fbWidth <= 0 || fbHeight <= 0)
				return;

			drawData.ScaleClipRects(drawData.FramebufferScale);

			GL.Enable(EnableCap.Blend);
			GL.BlendEquation(BlendEquationMode.FuncAdd);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			GL.Disable(EnableCap.CullFace);
			GL.Disable(EnableCap.DepthTest);
			GL.Enable(EnableCap.ScissorTest);
			GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);

			GL.Viewport(0, 0, fbWidth, fbHeight);

			Matrix4x4 projection = Matrix4x4.CreateOrthographicOffCenter(
					drawData.DisplayPos.X,
					drawData.DisplayPos.X + drawData.DisplaySize.X,
					drawData.DisplayPos.Y + drawData.DisplaySize.Y,
					drawData.DisplayPos.Y,
					-1f,
					1f);

			GL.UseProgram(_shaderProgram);
			GL.Uniform1(_uTextureLocation, 0);

			OpenTK.Mathematics.Matrix4 otkProjection = new(
					projection.M11, projection.M12, projection.M13, projection.M14,
					projection.M21, projection.M22, projection.M23, projection.M24,
					projection.M31, projection.M32, projection.M33, projection.M34,
					projection.M41, projection.M42, projection.M43, projection.M44);

			GL.UniformMatrix4(_uProjectionLocation, false, ref otkProjection);

			GL.BindVertexArray(_vao);

			for (int n = 0; n < drawData.CmdListsCount; n++)
			{
				ImDrawListPtr cmdList = drawData.CmdLists[n];

				int vertexSize = cmdList.VtxBuffer.Size * sizeof(ImDrawVert);
				if (vertexSize > _vertexBufferSize)
				{
					while (vertexSize > _vertexBufferSize)
						_vertexBufferSize *= 2;

					GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
					GL.BufferData(BufferTarget.ArrayBuffer, _vertexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
				}

				int indexElementSize = sizeof(ushort);
				int indexSize = cmdList.IdxBuffer.Size * indexElementSize;
				if (indexSize > _indexBufferSize)
				{
					while (indexSize > _indexBufferSize)
						_indexBufferSize *= 2;

					GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
					GL.BufferData(BufferTarget.ElementArrayBuffer, _indexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
				}

				GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
				GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, vertexSize, cmdList.VtxBuffer.Data);

				GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
				GL.BufferSubData(BufferTarget.ElementArrayBuffer, IntPtr.Zero, indexSize, cmdList.IdxBuffer.Data);

				int idxOffset = 0;
				int vtxOffset = 0;

				for (int cmdi = 0; cmdi < cmdList.CmdBuffer.Size; cmdi++)
				{
					ImDrawCmdPtr pcmd = cmdList.CmdBuffer[cmdi];

					if (pcmd.UserCallback != IntPtr.Zero)
						throw new NotSupportedException("ImGui user callbacks are not supported in this backend.");

					Texture2D texture = _textures.Resolve(pcmd.TextureId);
					texture.Bind(0);

					Vector4 clipRect = pcmd.ClipRect;

					GL.Scissor(
							(int)clipRect.X,
							(int)(fbHeight - clipRect.W),
							(int)(clipRect.Z - clipRect.X),
							(int)(clipRect.W - clipRect.Y));

					GL.DrawElementsBaseVertex(
							PrimitiveType.Triangles,
							(int)pcmd.ElemCount,
							DrawElementsType.UnsignedShort,
							idxOffset * sizeof(ushort),
							vtxOffset);

					idxOffset += (int)pcmd.ElemCount;
				}
			}

			GL.BindVertexArray(0);
			GL.UseProgram(0);
			GL.Disable(EnableCap.ScissorTest);
		}

		private static void CheckShaderCompile(int shader, string label)
		{
			GL.GetShader(shader, ShaderParameter.CompileStatus, out int ok);
			if (ok == 0)
			{
				string log = GL.GetShaderInfoLog(shader);
				throw new InvalidOperationException($"{label} compilation failed: {log}");
			}
		}

		private static void CheckProgramLink(int program, string label)
		{
			GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int ok);
			if (ok == 0)
			{
				string log = GL.GetProgramInfoLog(program);
				throw new InvalidOperationException($"{label} link failed: {log}");
			}
		}

		private void ThrowIfDisposed()
		{
			ObjectDisposedException.ThrowIf(_disposed, this);
		}

		public void Dispose()
		{
			if (_disposed)
				return;

			ImGuiIOPtr io = ImGui.GetIO();
			if (_fontTextureHandle.Id != 0)
				io.Fonts.SetTexID(IntPtr.Zero);

			_fontTexture?.Dispose();

			if (_ebo != 0) GL.DeleteBuffer(_ebo);
			if (_vbo != 0) GL.DeleteBuffer(_vbo);
			if (_vao != 0) GL.DeleteVertexArray(_vao);

			if (_shaderProgram != 0) GL.DeleteProgram(_shaderProgram);
			if (_vertexShader != 0) GL.DeleteShader(_vertexShader);
			if (_fragmentShader != 0) GL.DeleteShader(_fragmentShader);

			_disposed = true;
			GC.SuppressFinalize(this);
		}
	}
}
