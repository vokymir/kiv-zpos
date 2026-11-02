namespace Visualizer
{
	public enum SceneItemType
	{
		Root,
		Group,
		Entity,
		Light,
		Camera
	}

	/// <summary>
	/// A node in the scene hierarchy tree displayed in the Hierarchy panel.
	/// <see cref="Target"/> holds the underlying scene object (e.g. <c>Entity</c>, <c>Camera</c>, <c>AmbientLight</c>)
	/// that the Inspector will reflect when this node is selected.
	/// </summary>
	public sealed class SceneHierarchyItem
	{
		public required string Id { get; init; }
		public required string Name { get; set; }
		public required SceneItemType Type { get; init; }

		public object? Target { get; init; }

		public bool Visible { get; set; } = true;

		public List<SceneHierarchyItem> Children { get; } = [];
	}
}
