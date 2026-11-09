using System.Numerics;

namespace Renderer.Abstractions
{
	public sealed class MouseStateSnapshot
	{
		public Vector2 Position { get; init; }
		public Vector2 Delta { get; init; }
		public float WheelDelta { get; init; }

		public bool LeftDown { get; init; }
		public bool RightDown { get; init; }
		public bool MiddleDown { get; init; }
	}

	/// <summary>Immutable snapshot of pressed keys for a single frame.</summary>
	public sealed class KeyboardStateSnapshot(IEnumerable<KeyCode> down)
	{
		private readonly HashSet<KeyCode> _down = [.. down];
		/// <summary>Returns <c>true</c> if <paramref name="key"/> was held down this frame.</summary>
		public bool IsDown(KeyCode key) => _down.Contains(key);
	}

	public sealed class InputState
	{
		public MouseStateSnapshot Mouse { get; init; } = new();
		public KeyboardStateSnapshot Keyboard { get; init; } = new([]);
		public string TextInput { get; init; } = string.Empty;
	}
}
