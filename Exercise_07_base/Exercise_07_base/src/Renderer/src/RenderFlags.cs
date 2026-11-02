using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;

namespace Renderer
{
	/// <summary>Thin wrappers around GL state toggles (depth test, culling, polygon mode, etc.).</summary>
	public static class RenderFlags
	{
		private readonly static DebugProc DebugProcCallback = Window_DebugProc;
		private static void Window_DebugProc(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, IntPtr messagePtr, IntPtr userParam)
		{
			string message = Marshal.PtrToStringAnsi(messagePtr, length);
			var showMessage = source switch
			{
				DebugSource.DebugSourceApplication => false,
				_ => true,
			};
			if (showMessage)
			{
				switch (severity)
				{
					case DebugSeverity.DontCare:
						Console.WriteLine($"[DontCare] [{source}] {message}");
						break;
					case DebugSeverity.DebugSeverityNotification:
						//Logger?.LogDebug($"[{source}] {message}");
						break;
					case DebugSeverity.DebugSeverityHigh:
						Console.Error.WriteLine($"Error: [{source}] {message}");
						break;
					case DebugSeverity.DebugSeverityMedium:
						Console.WriteLine($"Warning: [{source}] {message}");
						break;
					case DebugSeverity.DebugSeverityLow:
						Console.WriteLine($"Info: [{source}] {message}");
						break;
					default:
						Console.WriteLine($"[default] [{source}] {message}");
						break;
				}
			}
		}

		/// <summary>Enables the OpenGL debug message callback that routes GL errors to the console.</summary>
		public static void EnableDebug()
		{
			GL.DebugMessageCallback(DebugProcCallback, IntPtr.Zero);
			GL.Enable(EnableCap.DebugOutput);
			GL.Enable(EnableCap.DebugOutputSynchronous);
		}

		public static void PointSize(float size) => GL.PointSize(size);

		public static void PolygonModeFill() => GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
		public static void PolygonModeLine() => GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
		public static void PolygonModePoint() => GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Point);

		public static void DisableLineSmoothing() => GL.Disable(EnableCap.LineSmooth);
		public static void EnableLineSmoothing() => GL.Enable(EnableCap.LineSmooth);

		public static void EnableProgramPointSize() => GL.Enable(EnableCap.ProgramPointSize);
		public static void DisableProgramPointSize() => GL.Disable(EnableCap.ProgramPointSize);

		public static void EnableCullFace() => GL.Enable(EnableCap.CullFace);
		public static void DisableCullFace() => GL.Disable(EnableCap.CullFace);

		public static void EnableDepthTest()
		{
			GL.Enable(EnableCap.DepthTest);
			GL.DepthFunc(DepthFunction.Less);
		}

		public static void DisableDepthTest()
		{
			GL.Disable(EnableCap.DepthTest);
		}

		public static void EnableBlend()
		{
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
		}

		public static void DisableBlend() => GL.Disable(EnableCap.Blend);
	}
}
