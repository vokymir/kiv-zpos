using System.Numerics;
using OTK = OpenTK.Mathematics;

namespace Renderer.Utils
{
	public static class MathConvert
	{
		public static OTK.Vector2 ToOpenTk(Vector2 v) => new(v.X, v.Y);
		public static OTK.Vector3 ToOpenTk(Vector3 v) => new(v.X, v.Y, v.Z);
		public static OTK.Vector4 ToOpenTk(Vector4 v) => new(v.X, v.Y, v.Z, v.W);
		public static OTK.Quaternion ToOpenTk(Quaternion q) => new(q.X, q.Y, q.Z, q.W);

		public static OTK.Matrix4 ToOpenTk(Matrix4x4 m) => new(
			m.M11, m.M12, m.M13, m.M14,
			m.M21, m.M22, m.M23, m.M24,
			m.M31, m.M32, m.M33, m.M34,
			m.M41, m.M42, m.M43, m.M44
		);

		public static Vector2 ToNumerics(OTK.Vector2 v) => new(v.X, v.Y);
		public static Vector3 ToNumerics(OTK.Vector3 v) => new(v.X, v.Y, v.Z);
		public static Vector4 ToNumerics(OTK.Vector4 v) => new(v.X, v.Y, v.Z, v.W);
		public static Quaternion ToNumerics(OTK.Quaternion q) => new(q.X, q.Y, q.Z, q.W);

		public static Matrix4x4 ToNumerics(OTK.Matrix4 m) => new(
			m.M11, m.M12, m.M13, m.M14,
			m.M21, m.M22, m.M23, m.M24,
			m.M31, m.M32, m.M33, m.M34,
			m.M41, m.M42, m.M43, m.M44
		);
	}
}
