using System.Numerics;

namespace Renderer.Abstractions
{
	public sealed class ImGuiFrameInput
	{
		public Vector2 DisplaySize { get; init; }
		public Vector2 FramebufferScale { get; init; } = Vector2.One;
		public float DeltaTime { get; init; }

		public Vector2 MousePosition { get; init; }
		public float MouseWheel { get; init; }
		public float MouseWheelH { get; init; }

		public bool MouseLeft { get; init; }
		public bool MouseRight { get; init; }
		public bool MouseMiddle { get; init; }

		public bool KeyCtrl { get; init; }
		public bool KeyShift { get; init; }
		public bool KeyAlt { get; init; }
		public bool KeySuper { get; init; }

		public string? TextInput { get; init; }
	}
}
