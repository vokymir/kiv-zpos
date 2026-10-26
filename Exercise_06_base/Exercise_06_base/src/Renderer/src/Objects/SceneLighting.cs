namespace Renderer.Objects
{
	/// <summary>Holds the ambient light and an arbitrary number of directional lights for a scene.</summary>
	public sealed class SceneLighting
	{
		public AmbientLight Ambient { get; } = new();

		private readonly List<DirectionalLight> _directionalLights = [];
		public IReadOnlyList<DirectionalLight> DirectionalLights => _directionalLights;

		/// <summary>Adds a directional light.</summary>
		public void AddDirectionalLight(DirectionalLight light)
		{
			ArgumentNullException.ThrowIfNull(light);
			_directionalLights.Add(light);
		}

		/// <summary>Removes the given directional light. Returns <c>true</c> if it was present.</summary>
		public bool RemoveDirectionalLight(DirectionalLight light)
		{
			ArgumentNullException.ThrowIfNull(light);
			return _directionalLights.Remove(light);
		}

		/// <summary>Removes all directional lights.</summary>
		public void ClearDirectionalLights()
		{
			_directionalLights.Clear();
		}
	}
}
