using System.Numerics;

namespace Renderer.Components
{
	/// <summary>
	/// Stores position, rotation, and scale and lazily computes the TRS model matrix.
	/// Setting any component marks the matrix dirty; the matrix is rebuilt on next access.
	/// </summary>
	public sealed class Transforms()
	{
		private Vector3 _scale = Vector3.One;
		private Vector3 _position = Vector3.Zero;
		private Quaternion _rotation = Quaternion.Identity;
		private bool _dirty = true;
		private Matrix4x4 _matrix = Matrix4x4.Identity;

		public Vector3 Scale
		{
			get => _scale;
			set
			{
				if (_scale != value)
				{
					_scale = value;
					_dirty = true;
				}
			}
		}

		public Vector3 Position
		{
			get => _position;
			set
			{
				if (_position != value)
				{
					_position = value;
					_dirty = true;
				}
			}
		}

		public Quaternion Rotation
		{
			get => _rotation;
			set
			{
				if (_rotation != value)
				{
					_rotation = Quaternion.Normalize(value);
					_dirty = true;
				}
			}
		}

		public Matrix4x4 Matrix
		{
			get
			{
				if (_dirty)
				{
					_matrix =
						Matrix4x4.CreateScale(_scale) *
						Matrix4x4.CreateFromQuaternion(_rotation) *
						Matrix4x4.CreateTranslation(_position);

					_dirty = false;
				}

				return _matrix;
			}
		}

		/// <summary>World-space forward direction (-Z rotated by the current rotation).</summary>
		public Vector3 Forward => Vector3.Transform(-Vector3.UnitZ, _rotation);
		/// <summary>World-space up direction (Y rotated by the current rotation).</summary>
		public Vector3 Up      => Vector3.Transform(Vector3.UnitY, _rotation);
		/// <summary>World-space right direction (X rotated by the current rotation).</summary>
		public Vector3 Right   => Vector3.Transform(Vector3.UnitX, _rotation);
	}
}
