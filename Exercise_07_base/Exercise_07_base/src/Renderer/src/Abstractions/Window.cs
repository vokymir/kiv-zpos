using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Numerics;

namespace Renderer.Abstractions
{
	/// <summary>
	/// Base class for an OpenTK-backed application window.
	/// Override <see cref="OnLoad"/>, <see cref="OnUpdate"/>, <see cref="OnRender"/>, and <see cref="OnClose"/>
	/// to implement application logic. Input state is available each frame via <see cref="Input"/>.
	/// </summary>
	public abstract class Window
	{
		private readonly GameWindow _glWindow;

		private Vector2 _lastMousePosition;
		private Vector2 _mouseDelta;
		private float _mouseWheelDelta;
		private string _textInput = string.Empty;

		// Sticky button state: set on MouseDown event, cleared after each input snapshot.
		// Ensures a click shorter than one frame is still captured.
		private bool _mouseLeftSticky;
		private bool _mouseRightSticky;
		private bool _mouseMiddleSticky;

		private static readonly (Keys GlKey, KeyCode KeyCode)[] _keyMap = [
			(Keys.A, KeyCode.A),
			(Keys.B, KeyCode.B),
			(Keys.C, KeyCode.C),
			(Keys.D, KeyCode.D),
			(Keys.E, KeyCode.E),
			(Keys.F, KeyCode.F),
			(Keys.G, KeyCode.G),
			(Keys.H, KeyCode.H),
			(Keys.I, KeyCode.I),
			(Keys.J, KeyCode.J),
			(Keys.K, KeyCode.K),
			(Keys.L, KeyCode.L),
			(Keys.M, KeyCode.M),
			(Keys.N, KeyCode.N),
			(Keys.O, KeyCode.O),
			(Keys.P, KeyCode.P),
			(Keys.Q, KeyCode.Q),
			(Keys.R, KeyCode.R),
			(Keys.S, KeyCode.S),
			(Keys.T, KeyCode.T),
			(Keys.U, KeyCode.U),
			(Keys.V, KeyCode.V),
			(Keys.W, KeyCode.W),
			(Keys.X, KeyCode.X),
			(Keys.Y, KeyCode.Y),
			(Keys.Z, KeyCode.Z),

			(Keys.D0, KeyCode.D0),
			(Keys.D1, KeyCode.D1),
			(Keys.D2, KeyCode.D2),
			(Keys.D3, KeyCode.D3),
			(Keys.D4, KeyCode.D4),
			(Keys.D5, KeyCode.D5),
			(Keys.D6, KeyCode.D6),
			(Keys.D7, KeyCode.D7),
			(Keys.D8, KeyCode.D8),
			(Keys.D9, KeyCode.D9),

			(Keys.LeftShift, KeyCode.LeftShift),
			(Keys.RightShift, KeyCode.RightShift),
			(Keys.LeftControl, KeyCode.LeftControl),
			(Keys.RightControl, KeyCode.RightControl),
			(Keys.LeftAlt, KeyCode.LeftAlt),
			(Keys.RightAlt, KeyCode.RightAlt),
			(Keys.LeftSuper, KeyCode.LeftSuper),
			(Keys.RightSuper, KeyCode.RightSuper),

			(Keys.Space, KeyCode.Space),
			(Keys.Enter, KeyCode.Enter),
			(Keys.Escape, KeyCode.Escape),
			(Keys.Tab, KeyCode.Tab),
			(Keys.Backspace, KeyCode.Backspace),

			(Keys.Left, KeyCode.Left),
			(Keys.Right, KeyCode.Right),
			(Keys.Up, KeyCode.Up),
			(Keys.Down, KeyCode.Down),

			(Keys.Insert, KeyCode.Insert),
			(Keys.Delete, KeyCode.Delete),
			(Keys.Home, KeyCode.Home),
			(Keys.End, KeyCode.End),
			(Keys.PageUp, KeyCode.PageUp),
			(Keys.PageDown, KeyCode.PageDown),
		];

		public float DeltaTime { get; private set; }

		public int Width
		{
			get => _glWindow.ClientSize.X;
			set => _glWindow.ClientSize = new(value, _glWindow.ClientSize.Y);
		}

		public int Height
		{
			get => _glWindow.ClientSize.Y;
			set => _glWindow.ClientSize = new(_glWindow.ClientSize.X, value);
		}

		public string Title
		{
			get => _glWindow.Title;
			set => _glWindow.Title = value;
		}

		public InputState Input { get; private set; } = new();

		/// <summary>Called once after the OpenGL context is created. Load resources here.</summary>
		protected abstract void OnLoad();
		/// <summary>Called each frame to issue draw calls.</summary>
		protected abstract void OnRender();
		/// <summary>Called each frame before <see cref="OnRender"/> to update game/application state.</summary>
		protected abstract void OnUpdate();
		/// <summary>Called when the window is about to close. Release resources here.</summary>
		protected abstract void OnClose();

		public Window(int width = 1280, int height = 800, string title = "Visualizer")
		{
			var gwSettings = GameWindowSettings.Default;
			_glWindow = new(gwSettings, new() { APIVersion = new Version(3, 3) });
			(Width, Height, Title) = (width, height, title);

			_glWindow.VSync = VSyncMode.On;

			_glWindow.Load += () =>
			{
				Render.ClearColor = new(0.1f, 0.2f, 0.4f, 1.0f);
				Render.Viewport(0, 0, Width, Height);

				_lastMousePosition = new Vector2(_glWindow.MousePosition.X, _glWindow.MousePosition.Y);

				OnLoad();
			};

			_glWindow.TextInput += e =>
			{
				_textInput += e.AsString;
			};

			_glWindow.MouseWheel += e =>
			{
				_mouseWheelDelta += e.OffsetY;
			};

			_glWindow.MouseDown += e =>
			{
				if (e.Button == MouseButton.Left)   _mouseLeftSticky   = true;
				if (e.Button == MouseButton.Right)  _mouseRightSticky  = true;
				if (e.Button == MouseButton.Middle) _mouseMiddleSticky = true;
			};

			_glWindow.Closing += _ =>
			{
				OnClose();
			};

			_glWindow.UpdateFrame += e =>
			{
				DeltaTime = (float)e.Time;
				UpdateInputSnapshot();
				OnUpdate();

				_mouseWheelDelta = 0f;
				_textInput = string.Empty;
			};

			_glWindow.RenderFrame += _ =>
			{
				OnRender();
			};

			_glWindow.Resize += e =>
			{
				Render.Viewport(0, 0, e.Width, e.Height);
			};
		}

		private void UpdateInputSnapshot()
		{
			var mouse = _glWindow.MouseState;
			var keyboard = _glWindow.KeyboardState;

			Vector2 mousePosition = new(mouse.Position.X, mouse.Position.Y);
			_mouseDelta = mousePosition - _lastMousePosition;
			_lastMousePosition = mousePosition;

			Input = new InputState
			{
				Mouse = new MouseStateSnapshot
				{
					Position = mousePosition,
					Delta = _mouseDelta,
					WheelDelta = _mouseWheelDelta,
					LeftDown   = mouse.IsButtonDown(MouseButton.Left)   || _mouseLeftSticky,
					RightDown  = mouse.IsButtonDown(MouseButton.Right)  || _mouseRightSticky,
					MiddleDown = mouse.IsButtonDown(MouseButton.Middle) || _mouseMiddleSticky,
				},
				Keyboard = new KeyboardStateSnapshot(GetPressedKeys(keyboard)),
				TextInput = _textInput
			};

			_mouseLeftSticky   = false;
			_mouseRightSticky  = false;
			_mouseMiddleSticky = false;
		}

		private static IEnumerable<KeyCode> GetPressedKeys(KeyboardState keyboard)
		{
			foreach (var (glKey, keyCode) in _keyMap)
			{
				if (keyboard.IsKeyDown(glKey))
					yield return keyCode;
			}
		}

		/// <summary>Starts the render loop. Blocks until the window is closed.</summary>
		public void Run() => _glWindow.Run();
		/// <summary>Requests that the window close at the end of the current frame.</summary>
		public void Close() => _glWindow.Close();
		/// <summary>Presents the back buffer to the screen.</summary>
		public void SwapBuffers() => _glWindow.SwapBuffers();
	}
}
