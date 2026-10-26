using System.Numerics;

namespace Core.Structures
{
	/// <summary>Axis-aligned bounding box in local (model) space.</summary>
	public readonly struct BoundingBox
	{
		public float MinX { get; }
		public float MinY { get; }
		public float MinZ { get; }
		public float MaxX { get; }
		public float MaxY { get; }
		public float MaxZ { get; }

		public float SizeX => MaxX - MinX;
		public float SizeY => MaxY - MinY;
		public float SizeZ => MaxZ - MinZ;

		public (float X, float Y, float Z) Center =>
			((MinX + MaxX) / 2f, (MinY + MaxY) / 2f, (MinZ + MaxZ) / 2f);

		public BoundingBox(float minX, float minY, float minZ, float maxX, float maxY, float maxZ)
			=> (MinX, MinY, MinZ, MaxX, MaxY, MaxZ) = (minX, minY, minZ, maxX, maxY, maxZ);

		/// <summary>Returns a world-space AABB by transforming all 8 corners by <paramref name="matrix"/>.</summary>
		public BoundingBox Transform(Matrix4x4 matrix)
		{
			Span<Vector3> corners =
			[
				Vector3.Transform(new Vector3(MinX, MinY, MinZ), matrix),
				Vector3.Transform(new Vector3(MaxX, MinY, MinZ), matrix),
				Vector3.Transform(new Vector3(MinX, MaxY, MinZ), matrix),
				Vector3.Transform(new Vector3(MaxX, MaxY, MinZ), matrix),
				Vector3.Transform(new Vector3(MinX, MinY, MaxZ), matrix),
				Vector3.Transform(new Vector3(MaxX, MinY, MaxZ), matrix),
				Vector3.Transform(new Vector3(MinX, MaxY, MaxZ), matrix),
				Vector3.Transform(new Vector3(MaxX, MaxY, MaxZ), matrix),
			];

			float minX = corners[0].X, minY = corners[0].Y, minZ = corners[0].Z;
			float maxX = minX,         maxY = minY,           maxZ = minZ;
			for (int i = 1; i < corners.Length; i++)
			{
				if (corners[i].X < minX) minX = corners[i].X; else if (corners[i].X > maxX) maxX = corners[i].X;
				if (corners[i].Y < minY) minY = corners[i].Y; else if (corners[i].Y > maxY) maxY = corners[i].Y;
				if (corners[i].Z < minZ) minZ = corners[i].Z; else if (corners[i].Z > maxZ) maxZ = corners[i].Z;
			}
			return new BoundingBox(minX, minY, minZ, maxX, maxY, maxZ);
		}

		/// <summary>Computes the AABB that encloses all <paramref name="vertices"/>.</summary>
		public static BoundingBox FromVertices(Vertex[] vertices)
		{
			if (vertices.Length == 0)
				return new BoundingBox(0, 0, 0, 0, 0, 0);

			float[] p0 = vertices[0].Channels["position"];
			float minX = p0[0], minY = p0[1], minZ = p0[2];
			float maxX = minX,  maxY = minY,  maxZ = minZ;

			for (int i = 1; i < vertices.Length; i++)
			{
				float[] p = vertices[i].Channels["position"];
				if (p[0] < minX) minX = p[0]; else if (p[0] > maxX) maxX = p[0];
				if (p[1] < minY) minY = p[1]; else if (p[1] > maxY) maxY = p[1];
				if (p[2] < minZ) minZ = p[2]; else if (p[2] > maxZ) maxZ = p[2];
			}

			return new BoundingBox(minX, minY, minZ, maxX, maxY, maxZ);
		}
	}
}
