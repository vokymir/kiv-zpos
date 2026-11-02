using System;
using System.Numerics;

namespace Core
{
    public static class Mathematics
    {
        private static readonly float _piOver180 = (float)Math.PI / 180f;
        private static readonly float _180OverPi = 180f / (float)Math.PI;

        /// <summary>Linearly interpolates between <paramref name="a"/> and <paramref name="b"/> by factor <paramref name="t"/>.</summary>
        public static float Lerp(float a, float b, float t) => a + (b - a) * t;

        /// <summary>Converts degrees to radians.</summary>
        public static float DegreesToRadians(float deg) => deg * _piOver180;

        /// <summary>Converts radians to degrees.</summary>
        public static float RadiansToDegrees(float rad) => rad * _180OverPi;

        /// <summary>Returns the angle in radians at vertex <paramref name="p2"/> formed by rays to <paramref name="p1"/> and <paramref name="p3"/>.</summary>
        public static float Angle(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            Vector3 v1 = p1 - p2;
            Vector3 v2 = p3 - p2;
            float denom = v1.Length() * v2.Length();
            if (denom <= 1e-12f) return 0f;

            float cosine = Math.Clamp(Vector3.Dot(v1, v2) / denom, -1f, 1f);
            return MathF.Acos(cosine);
        }

        /// <summary>Returns the area of the triangle with vertices <paramref name="a"/>, <paramref name="b"/>, and <paramref name="c"/>.</summary>
        public static float TriangleArea(Vector3 a, Vector3 b, Vector3 c)
        {
            return Vector3.Cross(a - b, c - b).Length() * 0.5f;
        }
    }
}