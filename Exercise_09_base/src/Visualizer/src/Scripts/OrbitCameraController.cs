using Core;
using Core.Structures;
using Renderer.Objects;
using System.Numerics;

namespace Visualizer.Scripts
{
	/// <summary>
	/// Spherical orbit camera: rotates around a target point at a given distance.
	/// Inputs (<see cref="Orbit"/>, <see cref="Pan"/>, <see cref="Zoom"/>, <see cref="FocusOnEntity"/>)
	/// write to desired state. Call <see cref="Update"/> each frame to exponentially interpolate
	/// the current state toward the desired state, then call <see cref="Apply"/> to push the
	/// result to a <see cref="Camera"/>.
	/// </summary>
	public sealed class OrbitCameraController
	{
		// ---- desired state (inputs write here) --------------------------------
		public Vector3 DesiredTarget   { get; set; } = Vector3.Zero;
		public float   DesiredDistance { get => _desiredDistance; set => _desiredDistance = Math.Clamp(value, MinDistance, MaxDistance); }
		public float   DesiredYaw      { get; set; } = Mathematics.DegreesToRadians(45f);
		public float   DesiredPitch    { get; set; } = Mathematics.DegreesToRadians(-30f);

		// ---- current (interpolated) state (read-only from outside) -----------
		public Vector3 Target   { get; private set; } = Vector3.Zero;
		public float   Distance { get => _distance; private set => _distance = Math.Clamp(value, MinDistance, MaxDistance); }
		public float   Yaw      { get; private set; } = Mathematics.DegreesToRadians(45f);
		public float   Pitch    { get; private set; } = Mathematics.DegreesToRadians(-30f);

		// ---- limits & tuning -------------------------------------------------
		public float MinDistance { get; set; } = 0.1f;
		public float MaxDistance { get; set; } = 1000f;

		public float OrbitSensitivity { get; set; } = 0.01f;
		public float PanSensitivity   { get; set; } = 0.0025f;
		public float ZoomSensitivity  { get; set; } = 0.25f;

		/// <summary>
		/// Controls how quickly the current state tracks the desired state.
		/// Higher values feel snappier; lower values feel floatier.
		/// Uses exponential decay: <c>current = Lerp(current, desired, 1 − exp(−speed × dt))</c>.
		/// </summary>
		public float InterpolationSpeed { get; set; } = 12f;

		/// <summary>
		/// How quickly zoom velocity bleeds off after scroll input stops.
		/// Higher values stop faster; lower values give more coast.
		/// </summary>
		public float ZoomDamping { get; set; } = 10f;

		private float _desiredDistance = 5f;
		private float _distance        = 5f;
		private float _zoomVelocity    = 0f;

		// ---- public API -------------------------------------------------------

		/// <summary>Snaps current state to desired state with no interpolation.</summary>
		public void SnapToDesired()
		{
			Target = DesiredTarget;
			_distance = _desiredDistance;
			Yaw   = DesiredYaw;
			Pitch = DesiredPitch;
		}

		/// <summary>
		/// Advances current state toward desired state using exponential decay.
		/// Call once per frame before <see cref="Apply"/>.
		/// </summary>
		public void Update(float deltaTime)
		{
			float t = 1f - MathF.Exp(-InterpolationSpeed * deltaTime);

			// Apply and damp zoom velocity before interpolating distance.
			if (_zoomVelocity != 0f)
			{
				_desiredDistance *= MathF.Exp(_zoomVelocity * deltaTime);
				_desiredDistance  = Math.Clamp(_desiredDistance, MinDistance, MaxDistance);
				_zoomVelocity    *= MathF.Exp(-ZoomDamping * deltaTime);
				if (MathF.Abs(_zoomVelocity) < 0.001f)
					_zoomVelocity = 0f;
			}

			Target    = Vector3.Lerp(Target, DesiredTarget, t);
			_distance = _distance + (_desiredDistance - _distance) * t;
			Yaw       = Yaw   + AngleDelta(Yaw,   DesiredYaw)   * t;
			Pitch     = Pitch + AngleDelta(Pitch, DesiredPitch) * t;
		}

		/// <summary>Adjusts desired yaw and pitch from a mouse drag delta (pixels). Pitch is clamped to ±89°.</summary>
		public void Orbit(Vector2 mouseDelta)
		{
			DesiredYaw   -= mouseDelta.X * OrbitSensitivity;
			DesiredPitch -= mouseDelta.Y * OrbitSensitivity;

			float limit = Mathematics.DegreesToRadians(89f);
			DesiredPitch = Math.Clamp(DesiredPitch, -limit, limit);
		}

		/// <summary>Moves the desired orbit target in the camera's local right/up plane.</summary>
		public void Pan(Vector2 mouseDelta)
		{
			Quaternion rotation = GetRotation(DesiredYaw, DesiredPitch);

			Vector3 right = Vector3.Transform(Vector3.UnitX, rotation);
			Vector3 up    = Vector3.Transform(Vector3.UnitY, rotation);

			float scale = DesiredDistance * PanSensitivity;

			DesiredTarget -= right * mouseDelta.X * scale;
			DesiredTarget += up    * mouseDelta.Y * scale;
		}

		/// <summary>Adds a zoom impulse proportional to <paramref name="wheelDelta"/>. The velocity decays smoothly over time.</summary>
		public void Zoom(float wheelDelta)
		{
			if (wheelDelta == 0f)
				return;

			_zoomVelocity -= wheelDelta * ZoomSensitivity;
		}

		/// <summary>
		/// Sets desired target and distance to frame <paramref name="entity"/>'s bounding box.
		/// The transition is driven by the normal per-frame interpolation.
		/// </summary>
		public void FocusOnEntity(Entity entity)
		{
			ArgumentNullException.ThrowIfNull(entity);

			BoundingBox box = entity.BoundingBox;
			(float cx, float cy, float cz) = box.Center;
			float diagonal = MathF.Sqrt(box.SizeX * box.SizeX + box.SizeY * box.SizeY + box.SizeZ * box.SizeZ);

			DesiredTarget   = new Vector3(cx, cy, cz);
			DesiredDistance = Math.Clamp(diagonal, MinDistance, MaxDistance);
		}

		public void FocusOnPoint(Vector3 point, float radius)
		{
			DesiredTarget   = point;
			DesiredDistance = Math.Clamp(radius * 4f, MinDistance, MaxDistance);
		}

		/// <summary>Writes the current interpolated state to <paramref name="camera"/>.</summary>
		public void Apply(Camera camera)
		{
			ArgumentNullException.ThrowIfNull(camera);

			Quaternion rotation = GetRotation(Yaw, Pitch);
			Vector3 forward  = Vector3.Transform(-Vector3.UnitZ, rotation);
			Vector3 position = Target - forward * Distance;

			camera.Transforms.Position = position;
			camera.Transforms.Rotation = rotation;
		}

		// ---- helpers ----------------------------------------------------------

		private static Quaternion GetRotation(float yaw, float pitch)
		{
			Quaternion yawQ   = Quaternion.CreateFromAxisAngle(Vector3.UnitY, yaw);
			Quaternion pitchQ = Quaternion.CreateFromAxisAngle(Vector3.UnitX, pitch);
			return Quaternion.Normalize(yawQ * pitchQ);
		}

		/// <summary>Returns the shortest signed difference between two angles.</summary>
		private static float AngleDelta(float current, float desired)
		{
			float d = (desired - current) % (2f * MathF.PI);
			if (d >  MathF.PI) d -= 2f * MathF.PI;
			if (d < -MathF.PI) d += 2f * MathF.PI;
			return d;
		}
	}
}
