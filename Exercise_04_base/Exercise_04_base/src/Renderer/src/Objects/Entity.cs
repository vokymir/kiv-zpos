using Core.Structures;
using Renderer.Components;

namespace Renderer.Objects
{
	public class Entity()
	{
		public Mesh?      Mesh       { get; set; }
		public Material?  Material   { get; set; }
		public Transforms Transforms { get; } = new();

		/// <summary>Axis-aligned bounding box in world space, computed from mesh vertices transformed by the entity's model matrix.</summary>
		public BoundingBox BoundingBox =>
			Mesh is { Vertices.Length: > 0 }
				? BoundingBox.FromVertices(Mesh.Vertices).Transform(Transforms.Matrix)
				: new BoundingBox(0, 0, 0, 0, 0, 0);
	}
}
