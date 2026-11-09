using System.Numerics;
using Renderer.Wrappers.Shaders;

namespace Renderer.Components
{
	public interface IMaterialParameter
	{
		string Name { get; }
		Type ValueType { get; }
		void Apply(ShaderProgram shader);
	}

    public sealed class MaterialParameter<T> : IMaterialParameter where T : unmanaged
    {
        public string Name { get; }
        public Type ValueType => typeof(T);
		public T Value { get; set; }

		public MaterialParameter(string name, T value)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentException("Parameter name cannot be null or empty.", nameof(name));

			Name = name;
			Value = value;
		}

        public void Apply(ShaderProgram shader)
        {
			ArgumentNullException.ThrowIfNull(shader);

			switch (Value)
			{
				case int v:
					shader.SetUniformImmediate(Name, v);
					break;

				case float v:
					shader.SetUniformImmediate(Name, v);
					break;

				case bool v:
					shader.SetUniformImmediate(Name, v);
					break;

				case Vector2 v:
					shader.SetUniformImmediate(Name, v);
					break;

				case Vector3 v:
					shader.SetUniformImmediate(Name, v);
					break;

				case Vector4 v:
					shader.SetUniformImmediate(Name, v);
					break;

				case Matrix4x4 v:
					shader.SetUniformImmediate(Name, v);
					break;

				default:
					throw new NotSupportedException($"Material parameter type '{typeof(T).FullName}' is not supported.");
			}
        }
    }
}
