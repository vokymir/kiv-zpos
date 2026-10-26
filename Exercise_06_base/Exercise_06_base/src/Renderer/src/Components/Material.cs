using Renderer.Wrappers.Shaders;

namespace Renderer.Components
{
	/// <summary>Binds a <see cref="ShaderProgram"/> and manages the typed uniform parameters uploaded to it each frame.</summary>
	public sealed class Material(ShaderProgram shader)
	{
		private readonly Dictionary<string, IMaterialParameter> _parameters = new(StringComparer.Ordinal);
		public IReadOnlyDictionary<string, IMaterialParameter> Parameters => _parameters;

		public ShaderProgram Shader { get; } = shader ?? throw new ArgumentNullException(nameof(shader));

		/// <summary>
		/// Sets or updates a typed shader uniform.
		/// Throws if a parameter with the same name already exists but with a different type.
		/// </summary>
		public void SetParameter<T>(string name, T value) where T : unmanaged
		{
			ValidateParameterName(name);

			if (_parameters.TryGetValue(name, out IMaterialParameter? existing))
			{
				if (existing is MaterialParameter<T> typed)
				{
					typed.Value = value;
					return;
				}

				throw new InvalidOperationException($"Material parameter '{name}' already exists as '{existing.ValueType.Name}', not '{typeof(T).Name}'.");
			}

			_parameters.Add(name, new MaterialParameter<T>(name, value));
		}

		/// <summary>Removes the parameter with the given name. Returns <c>true</c> if it existed.</summary>
		public bool RemoveParameter(string name)
		{
			ValidateParameterName(name);
			return _parameters.Remove(name);
		}

		/// <summary>Returns <c>true</c> if a parameter with the given name has been set.</summary>
		public bool HasParameter(string name)
		{
			ValidateParameterName(name);
			return _parameters.ContainsKey(name);
		}

		/// <summary>Attempts to retrieve a typed parameter value. Returns <c>false</c> if not found or the type does not match.</summary>
		public bool TryGetParameter<T>(string name, out T value) where T : unmanaged
		{
			ValidateParameterName(name);

			if (_parameters.TryGetValue(name, out IMaterialParameter? parameter) && parameter is MaterialParameter<T> typed)
			{
				value = typed.Value;
				return true;
			}

			value = default;
			return false;
		}

		/// <summary>Removes all parameters.</summary>
		public void ClearParameters()
		{
			_parameters.Clear();
		}

		/// <summary>Binds the shader and uploads all current parameter values as uniforms.</summary>
		public void Apply()
		{
			Shader.Bind();

			foreach (IMaterialParameter parameter in _parameters.Values)
				parameter.Apply(Shader);
		}

		private static void ValidateParameterName(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentException("Parameter name cannot be null or empty.", nameof(name));
		}
	}
}
