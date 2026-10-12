using OTK = OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL;
using Renderer.Utils;
using System.Numerics;

namespace Renderer.Wrappers.Shaders
{
	public sealed class ShaderProgram : GLDisposable
	{
		public int Handle { get; }

		private readonly Dictionary<int, Shader> _shaders = [];
		private readonly Dictionary<string, int> _uniformLocations = new(StringComparer.Ordinal);

		public ShaderProgram()
		{
			Handle = GL.CreateProgram();
		}

		public ShaderProgram(params Shader[] shaders)
		{
			Handle = GL.CreateProgram();

			foreach (var shader in shaders)
				AttachShader(shader);
		}

		public bool Bind()
		{
			ThrowIfDisposed();
			GL.UseProgram(Handle);
			return true;
		}

		public bool Unbind()
		{
			ThrowIfDisposed();
			GL.UseProgram(0);
			return true;
		}

		public void Link()
		{
			ThrowIfDisposed();

			GL.LinkProgram(Handle);
			GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int code);

			_uniformLocations.Clear();

			if (code != (int)All.True)
			{
				string message = GL.GetProgramInfoLog(Handle);
				throw new Exception($"Error linking Program {Handle}: {message}");
			}
		}

		public void AttachShader(Shader shader)
		{
			ArgumentNullException.ThrowIfNull(shader);
			ThrowIfDisposed();

			if (!shader.IsCompiled)
				throw new InvalidOperationException($"Cannot attach an uncompiled shader '{nameof(shader)}'.");

			if (_shaders.ContainsKey(shader.Handle))
				return;

			_shaders.Add(shader.Handle, shader);
			GL.AttachShader(Handle, shader.Handle);
		}

		public void DetachShader(Shader shader)
		{
			ArgumentNullException.ThrowIfNull(shader);
			ThrowIfDisposed();

			if (!_shaders.ContainsKey(shader.Handle))
				return;

			_shaders.Remove(shader.Handle);
			GL.DetachShader(Handle, shader.Handle);
		}

		public int GetUniformLocation(string name)
		{
			ThrowIfDisposed();
			ValidateUniformName(name);

			if (_uniformLocations.TryGetValue(name, out int location))
				return location;

			int resolved = GL.GetUniformLocation(Handle, name);
			_uniformLocations[name] = resolved;
			return resolved;
		}

		public void SetUniformImmediate(string name, int value)
		{
			SetUniformImmediate(GetUniformLocation(name), value);
		}

		public void SetUniformImmediate(string name, float value)
		{
			SetUniformImmediate(GetUniformLocation(name), value);
		}

		public void SetUniformImmediate(string name, bool value)
		{
			SetUniformImmediate(GetUniformLocation(name), value ? 1 : 0);
		}

		public void SetUniformImmediate(string name, Vector2 value)
		{
			SetUniformImmediate(GetUniformLocation(name), value);
		}

		public void SetUniformImmediate(string name, Vector3 value)
		{
			SetUniformImmediate(GetUniformLocation(name), value);
		}

		public void SetUniformImmediate(string name, Vector4 value)
		{
			SetUniformImmediate(GetUniformLocation(name), value);
		}

		public void SetUniformImmediate(string name, Matrix4x4 value, bool transpose = false)
		{
			SetUniformImmediate(GetUniformLocation(name), value, transpose);
		}

		public void SetUniformImmediate(int location, int value)
		{
			ThrowIfDisposed();

			if (location < 0)
				return;

			GL.Uniform1(location, value);
		}

		public void SetUniformImmediate(int location, float value)
		{
			ThrowIfDisposed();

			if (location < 0)
				return;

			GL.Uniform1(location, value);
		}

		public void SetUniformImmediate(int location, Vector2 value)
		{
			ThrowIfDisposed();

			if (location < 0)
				return;

			GL.Uniform2(location, MathConvert.ToOpenTk(value));
		}

		public void SetUniformImmediate(int location, Vector3 value)
		{
			ThrowIfDisposed();

			if (location < 0)
				return;

			GL.Uniform3(location, MathConvert.ToOpenTk(value));
		}

		public void SetUniformImmediate(int location, Vector4 value)
		{
			ThrowIfDisposed();

			if (location < 0)
				return;

			GL.Uniform4(location, MathConvert.ToOpenTk(value));
		}

		public void SetUniformImmediate(int location, Matrix4x4 value, bool transpose = false)
		{
			ThrowIfDisposed();

			if (location < 0)
				return;

			OTK.Matrix4 m = MathConvert.ToOpenTk(value);
			GL.UniformMatrix4(location, transpose, ref m);
		}

		private static void ValidateUniformName(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentException("Uniform name cannot be null, empty, or whitespace.", nameof(name));
		}

		protected override void GLDispose()
		{
			_uniformLocations.Clear();
			_shaders.Clear();

			GL.DeleteProgram(Handle);
		}
	}
}
