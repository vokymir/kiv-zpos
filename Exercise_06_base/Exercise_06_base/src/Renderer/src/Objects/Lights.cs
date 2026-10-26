using System.Numerics;

namespace Renderer.Objects
{
	/// <summary>A uniform ambient light applied to all surfaces regardless of direction.</summary>
	public sealed class AmbientLight()
	{
		public Vector3 Color     { get; set; } = Vector3.One;
		public float   Intensity { get; set; } = 0.1f;
	}

	/// <summary>An infinitely distant directional light. The direction vector is always normalised.</summary>
	public sealed class DirectionalLight()
	{
		private Vector3 _direction = Vector3.Normalize(new Vector3(-0.2f, -1.0f, -0.3f));

		public Vector3 Color     { get; set; } = Vector3.One;
		public float   Intensity { get; set; } = 1.0f;

		public Vector3 Direction
		{
			get => _direction;
			set
			{
				if (value.LengthSquared() <= 0.000001f)
					throw new ArgumentException("Directional light direction cannot be zero.", nameof(value));

				_direction = Vector3.Normalize(value);
			}
		}
	}
}
