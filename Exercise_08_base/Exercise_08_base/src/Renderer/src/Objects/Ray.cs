using System.Numerics;
using Core.Structures;
using Renderer.Components;

namespace Renderer.Objects
{
	public readonly struct Ray(Vector3 origin, Vector3 direction)
	{
		public Vector3 Origin    { get; } = origin;
		public Vector3 Direction { get; } = Vector3.Normalize(direction);

		/// <summary> Point along the ray at parameter t. </summary>
		public Vector3 At(float t) => Origin + Direction * t;

		// ── Screen-space factory ────────────────────────────────────────────

		/// <summary>
		/// Constructs a ray from normalised device coordinates [-1,1] and a camera.
		/// ndcX/ndcY are already in NDC space (e.g. mouse mapped from viewport pixels).
		/// </summary>
		public static Ray FromNdc(float ndcX, float ndcY, Camera camera, float aspectRatio)
		{
			Matrix4x4 proj = camera.GetProjectionMatrix(aspectRatio);
			Matrix4x4 view = camera.GetViewMatrix();

			Matrix4x4.Invert(proj, out Matrix4x4 invProj);
			Matrix4x4.Invert(view, out Matrix4x4 invView);

			// View-space ray direction
			Vector4 rayView = Vector4.Transform(new Vector4(ndcX, ndcY, -1f, 1f), invProj);
			rayView = new Vector4(rayView.X, rayView.Y, -1f, 0f);

			// World-space ray direction
			Vector4 rayWorld = Vector4.Transform(rayView, invView);
			Vector3 direction = Vector3.Normalize(new Vector3(rayWorld.X, rayWorld.Y, rayWorld.Z));

			return new Ray(camera.Transforms.Position, direction);
		}

		// ── Closest-point queries ───────────────────────────────────────────

		/// <summary>
		/// Parameter t of the point on the ray closest to <paramref name="point"/>.
		/// Clamped to 0 so negative (behind-origin) results are excluded.
		/// </summary>
		public float ClosestT(Vector3 point) =>
			MathF.Max(0f, Vector3.Dot(point - Origin, Direction));

		/// <summary>
		/// Perpendicular distance from <paramref name="point"/> to the ray.
		/// </summary>
		public float DistanceTo(Vector3 point)
		{
			float t = ClosestT(point);
			return Vector3.Distance(point, At(t));
		}

		// ── Intersection tests ──────────────────────────────────────────────

		/// <summary>
		/// Möller–Trumbore ray/triangle intersection.
		/// Returns true and sets <paramref name="t"/> to the distance if hit (t > 0).
		/// </summary>
		public bool IntersectsTriangle(
			Vector3 v0, Vector3 v1, Vector3 v2,
			out float t,
			float epsilon = 1e-6f)
		{
			t = 0f;

			Vector3 edge1 = v1 - v0;
			Vector3 edge2 = v2 - v0;
			Vector3 h = Vector3.Cross(Direction, edge2);
			float   a = Vector3.Dot(edge1, h);

			if (MathF.Abs(a) < epsilon)
				return false; // parallel

			float   f = 1f / a;
			Vector3 s = Origin - v0;
			float   u = f * Vector3.Dot(s, h);

			if (u < 0f || u > 1f)
				return false;

			Vector3 q = Vector3.Cross(s, edge1);
			float   v = f * Vector3.Dot(Direction, q);

			if (v < 0f || u + v > 1f)
				return false;

			t = f * Vector3.Dot(edge2, q);
			return t > epsilon;
		}

		/// <summary>
		/// Tests the ray against every triangle in a mesh (object space).
		/// Returns true if any triangle is hit; <paramref name="t"/> is the nearest hit distance.
		/// </summary>
		public bool IntersectsMesh(Mesh mesh, out float t)
		{
			t = float.MaxValue;
			bool hit = false;

			Vertex[] verts   = mesh.Vertices;
			int[]    indices = mesh.Indices;

			for (int i = 0; i + 2 < indices.Length; i += 3)
			{
				float[] p0 = verts[indices[i]].Channels["position"];
				float[] p1 = verts[indices[i + 1]].Channels["position"];
				float[] p2 = verts[indices[i + 2]].Channels["position"];

				if (IntersectsTriangle(
						new Vector3(p0[0], p0[1], p0[2]),
						new Vector3(p1[0], p1[1], p1[2]),
						new Vector3(p2[0], p2[1], p2[2]),
						out float tHit) && tHit < t)
				{
					t   = tHit;
					hit = true;
				}
			}

			return hit;
		}

		/// <summary>
		/// Tests the ray (world space) against an entity, accounting for its model transform.
		/// Returns true if hit; <paramref name="t"/> is world-space distance to the nearest triangle.
		/// </summary>
		public bool IntersectsEntity(Entity entity, out float t)
		{
			t = float.MaxValue;

			if (entity.Mesh is null)
				return false;

			// Transform ray into object space
			Matrix4x4 model = entity.Transforms.Matrix;
			Matrix4x4.Invert(model, out Matrix4x4 invModel);

			Vector3 originOS    = Vector3.Transform(Origin, invModel);
			Vector3 directionOS = Vector3.Normalize(
				Vector3.TransformNormal(Direction, invModel));

			Ray objectSpaceRay = new(originOS, directionOS);
			return objectSpaceRay.IntersectsMesh(entity.Mesh, out t);
		}

		/// <summary>
		/// Ray/axis-aligned bounding box intersection (slab method).
		/// Returns true if hit; <paramref name="tMin"/> is entry distance.
		/// </summary>
		public bool IntersectsAabb(Vector3 min, Vector3 max, out float tMin)
		{
			tMin = float.MinValue;
			float tMax = float.MaxValue;

			for (int i = 0; i < 3; i++)
			{
				float origin = i == 0 ? Origin.X : i == 1 ? Origin.Y : Origin.Z;
				float dir    = i == 0 ? Direction.X : i == 1 ? Direction.Y : Direction.Z;
				float bMin   = i == 0 ? min.X : i == 1 ? min.Y : min.Z;
				float bMax   = i == 0 ? max.X : i == 1 ? max.Y : max.Z;

				if (MathF.Abs(dir) < 1e-8f)
				{
					if (origin < bMin || origin > bMax) return false;
					continue;
				}

				float t1 = (bMin - origin) / dir;
				float t2 = (bMax - origin) / dir;
				if (t1 > t2) (t1, t2) = (t2, t1);

				tMin = MathF.Max(tMin, t1);
				tMax = MathF.Min(tMax, t2);

				if (tMin > tMax) return false;
			}

			return tMin >= 0f || tMax >= 0f;
		}

		/// <summary>
		/// Ray/sphere intersection. Returns true if hit; <paramref name="t"/> is nearest positive distance.
		/// </summary>
		public bool IntersectsSphere(Vector3 center, float radius, out float t)
		{
			t = 0f;
			Vector3 oc = Origin - center;
			float   b  = Vector3.Dot(oc, Direction);
			float   c  = Vector3.Dot(oc, oc) - radius * radius;
			float   d  = b * b - c;

			if (d < 0f) return false;

			float sqrtD = MathF.Sqrt(d);
			float t0    = -b - sqrtD;
			float t1    = -b + sqrtD;

			if (t0 > 0f) { t = t0; return true; }
			if (t1 > 0f) { t = t1; return true; }
			return false;
		}
	}
}
