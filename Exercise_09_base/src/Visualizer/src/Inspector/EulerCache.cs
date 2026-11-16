using Core;
using Renderer.Components;
using System.Numerics;

namespace Visualizer.Inspector;

// Shared euler↔quaternion helpers and per-Transforms euler cache.
// The cache avoids gimbal-lock discontinuities when converting Rotation → euler each frame.
public static class EulerCache
{
    private static readonly Dictionary<Transforms, Vector3> _cache = [];

    public static Vector3 GetEulerDegrees(Transforms t)
    {
        Quaternion current = t.Rotation;

        if (_cache.TryGetValue(t, out Vector3 cached))
        {
            if (QuaternionsEqual(current, FromEuler(cached)))
                return cached;
        }

        Vector3 fresh = ToEuler(current);
        _cache[t] = fresh;
        return fresh;
    }

    public static void SetEulerDegrees(Transforms t, Vector3 euler)
    {
        _cache[t] = euler;
        t.Rotation = FromEuler(euler);
    }

    public static void Evict(Transforms t) => _cache.Remove(t);

    public static Quaternion FromEuler(Vector3 degrees) =>
        Quaternion.CreateFromYawPitchRoll(
            Mathematics.DegreesToRadians(degrees.Y),
            Mathematics.DegreesToRadians(degrees.X),
            Mathematics.DegreesToRadians(degrees.Z));

    public static Vector3 ToEuler(Quaternion q)
    {
        q = Quaternion.Normalize(q);

        float sinrCosp = 2f * (q.W * q.X + q.Y * q.Z);
        float cosrCosp = 1f - 2f * (q.X * q.X + q.Y * q.Y);
        float roll = MathF.Atan2(sinrCosp, cosrCosp);

        float sinp = 2f * (q.W * q.Y - q.Z * q.X);
        float pitch = MathF.Abs(sinp) >= 1f
            ? MathF.CopySign(MathF.PI / 2f, sinp)
            : MathF.Asin(sinp);

        float sinyCosp = 2f * (q.W * q.Z + q.X * q.Y);
        float cosyCosp = 1f - 2f * (q.Y * q.Y + q.Z * q.Z);
        float yaw = MathF.Atan2(sinyCosp, cosyCosp);

        return new Vector3(
            Mathematics.RadiansToDegrees(pitch),
            Mathematics.RadiansToDegrees(yaw),
            Mathematics.RadiansToDegrees(roll));
    }

    private static bool QuaternionsEqual(Quaternion a, Quaternion b)
    {
        float dot = a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W;
        return MathF.Abs(dot) >= 1f - 1e-5f;
    }
}
