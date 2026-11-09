using System.Collections.Concurrent;
using Core.IO;
using Core.Structures;
using Renderer.Wrappers.Model;

namespace Visualizer
{
	/// <summary>
	/// Entry point for embedding the visualizer in an external application.
	/// All public methods are thread-safe.
	/// </summary>
	public sealed class VisualizerSession : IDisposable
	{
		private readonly ConcurrentQueue<IVisualizerCommand> _queue = new();
		private readonly AppWindow _window;

		private readonly List<FrameData> _pendingFrames = [];
		private bool _isBuildingAnimation;

		private VisualizerSession()
		{
			_window = new AppWindow(new ObjImporter(), null, _queue);
		}

		/// <summary>
		/// Opens the visualizer window on the calling thread and runs <paramref name="userCode"/>
		/// on a background thread, passing a <see cref="VisualizerSession"/> for data submission.
		/// Blocks until the window is closed.
		/// </summary>
		/// <remarks>
		/// The window must run on the main thread on Linux/X11 (GLX requirement). Use this method
		/// instead of <see cref="Launch"/> when targeting Linux.
		/// </remarks>
		public static void Run(Action<VisualizerSession> userCode)
		{
			VisualizerSession session = new();
			Thread userThread = new(() => userCode(session)) { IsBackground = true, Name = "VisualizerUser" };
			userThread.Start();
			session._window.Run();
		}

		/// <summary>
		/// Replaces the current mesh, or appends an animation frame if called after
		/// <see cref="BeginAnimation"/> and before <see cref="CommitAnimation"/>.
		/// Takes effect on the next render frame.
		/// </summary>
		/// <param name="layout">
		/// Optional mapping of vertex channels to GPU attribute slots. If <see langword="null"/>,
		/// defaults to position → attribute 0, normal → attribute 1, unless overridden by
		/// <paramref name="sceneName"/>'s <c>.scene</c> file.
		/// </param>
		/// <param name="sceneName">
		/// Optional identifier for this mesh's scene state. When set (and changed from the
		/// previous call), a <c>&lt;sceneName&gt;.scene</c> sidecar is loaded/created next to
		/// <c>Assets/Models/</c> (or at an absolute path), and its vertex layout, material,
		/// camera, and lighting are applied. The same sidecar can be edited and saved via the UI.
		/// Ignored while building an animation.
		/// </param>
		public void SetMesh(Vertex[] vertices, int[] indices, List<VertexChannelSpec>? layout = null, string? sceneName = null)
		{
			if (_isBuildingAnimation)
				_pendingFrames.Add(new FrameData(vertices, indices, layout));
			else
				_queue.Enqueue(new SetMeshCommand(vertices, indices, layout, sceneName));
		}

		/// <summary>
		/// Replaces the current animation with all frames at once.
		/// Each element of <paramref name="frames"/> and <paramref name="indices"/> is one frame.
		/// Takes effect on the next render frame.
		/// </summary>
		/// <param name="layout">
		/// Optional mapping of vertex channels to GPU attribute slots, applied to every frame.
		/// If <see langword="null"/>, defaults to position → attribute 0, normal → attribute 1,
		/// unless overridden by <paramref name="sceneName"/>'s <c>.scene</c> file.
		/// </param>
		/// <param name="sceneName">See <see cref="SetMesh"/>.</param>
		public void SetAnimation(Vertex[][] frames, int[][] indices, List<VertexChannelSpec>? layout = null, string? sceneName = null)
		{
			FrameData[] data = new FrameData[frames.Length];
			for (int i = 0; i < frames.Length; i++)
				data[i] = new FrameData(frames[i], indices[i], layout);
			_queue.Enqueue(new SetAnimationCommand(data, sceneName));
		}

		/// <summary>
		/// Clears the pending frame buffer and switches <see cref="SetMesh"/> into appending
		/// frames to it instead of replacing the current mesh.
		/// </summary>
		public void BeginAnimation()
		{
			_pendingFrames.Clear();
			_isBuildingAnimation = true;
		}

		/// <summary>
		/// Uploads all frames accumulated since <see cref="BeginAnimation"/> to the visualizer
		/// and switches <see cref="SetMesh"/> back to replacing the current mesh.
		/// Does not start playback — call <see cref="Play"/> explicitly.
		/// </summary>
		/// <param name="sceneName">See <see cref="SetMesh"/>.</param>
		public void CommitAnimation(string? sceneName = null)
		{
			_isBuildingAnimation = false;
			_queue.Enqueue(new SetAnimationCommand([.. _pendingFrames], sceneName));
		}

		/// <summary>Starts animation playback.</summary>
		public void Play()  => _queue.Enqueue(new PlayCommand());

		/// <summary>Pauses animation playback.</summary>
		public void Pause() => _queue.Enqueue(new PauseCommand());

		/// <summary>Stops and resets animation playback to frame 0.</summary>
		public void Stop()  => _queue.Enqueue(new StopCommand());

		/// <summary>Closes the visualizer window.</summary>
		public void Dispose() => _window.Close();
	}
}
