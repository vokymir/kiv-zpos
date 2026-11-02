using Core;
using Renderer.Components;
using System.Numerics;

namespace Renderer.Objects
{
	/// <summary>A perspective camera defined by its <see cref="Transforms"/>, field of view, and clip planes.</summary>
	public class Camera()
	{
		private float _fov = Mathematics.DegreesToRadians(60f);

		public Transforms Transforms { get; } = new();
		/// <summary>Vertical field of view in radians. Clamped to a minimum of 0.1 rad.</summary>
		public float FieldOfView     { get => _fov; set => _fov = value > 0.0f ? value : 0.1f; }
		public float NearPlane       { get; set; } = 0.1f;
		public float FarPlane        { get; set; } = 1000f;

		/// <summary>Returns the view matrix for the camera's current position and orientation.</summary>
		public Matrix4x4 GetViewMatrix()
		{
			Vector3 eye = Transforms.Position;
			Vector3 target = eye + Transforms.Forward;
			return Matrix4x4.CreateLookAt(eye, target, Transforms.Up);
		}

		/// <summary>Returns the perspective projection matrix for the given <paramref name="aspectRatio"/> (width / height).</summary>
		public Matrix4x4 GetProjectionMatrix(float aspectRatio)
		{
			return Matrix4x4.CreatePerspectiveFieldOfView(FieldOfView, aspectRatio, NearPlane, FarPlane);
		}
	}
}
