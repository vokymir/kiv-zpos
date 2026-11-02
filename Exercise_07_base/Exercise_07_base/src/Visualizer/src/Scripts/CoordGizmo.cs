using ImGuiNET;
using Renderer.Objects;
using System.Numerics;

namespace Visualizer.Scripts
{
	/// <summary>Draws a XYZ orientation gizmo in the corner of the scene viewport using the ImGui draw list.</summary>
	internal static class CoordGizmo
	{
		private const float Radius     = 50f;  // half-size of gizmo area
		private const float AxisLength = 40f;  // length of each axis line
		private const float Padding    = 12f;  // distance from viewport edge
		private const float LineThick  = 2.5f;
		private const float NegAlpha   = 0.35f;

		private static readonly uint ColorX    = ImGui.ColorConvertFloat4ToU32(new Vector4(0.95f, 0.25f, 0.25f, 1.00f));
		private static readonly uint ColorY    = ImGui.ColorConvertFloat4ToU32(new Vector4(0.30f, 0.85f, 0.30f, 1.00f));
		private static readonly uint ColorZ    = ImGui.ColorConvertFloat4ToU32(new Vector4(0.25f, 0.50f, 1.00f, 1.00f));
		private static readonly uint ColorXNeg = ImGui.ColorConvertFloat4ToU32(new Vector4(0.95f, 0.25f, 0.25f, NegAlpha));
		private static readonly uint ColorYNeg = ImGui.ColorConvertFloat4ToU32(new Vector4(0.30f, 0.85f, 0.30f, NegAlpha));
		private static readonly uint ColorZNeg = ImGui.ColorConvertFloat4ToU32(new Vector4(0.25f, 0.50f, 1.00f, NegAlpha));

		public static void Draw(ImDrawListPtr draw, Vector2 imageOrigin, Vector2 viewportSize, Camera camera)
		{
			// Place gizmo centre in the bottom-left corner of the viewport
			Vector2 centre = imageOrigin + new Vector2(Radius + Padding, viewportSize.Y - Radius - Padding);

			// Extract only the rotation part of the view matrix so the gizmo
			// shows orientation without translation or zoom effects.
			Matrix4x4 view = camera.GetViewMatrix();

			// World-space axes projected to 2D (orthographic, Y flipped for screen space)
			Vector2 xAxis = Project(new Vector3(1, 0, 0), view);
			Vector2 yAxis = Project(new Vector3(0, 1, 0), view);
			Vector2 zAxis = Project(new Vector3(0, 0, 1), view);

			// Draw back-facing (negative) axes first so positive axes render on top
			DrawAxis(draw, centre, -xAxis, ColorXNeg, null);
			DrawAxis(draw, centre, -yAxis, ColorYNeg, null);
			DrawAxis(draw, centre, -zAxis, ColorZNeg, null);

			DrawAxis(draw, centre, xAxis, ColorX, "X");
			DrawAxis(draw, centre, yAxis, ColorY, "Y");
			DrawAxis(draw, centre, zAxis, ColorZ, "Z");
		}

		private static Vector2 Project(Vector3 worldAxis, Matrix4x4 view)
		{
			// Rotate the world axis into camera space
			Vector3 cam = Vector3.TransformNormal(worldAxis, view);
			// Map to 2D screen: X stays, Y is flipped (screen Y grows downward)
			return new Vector2(cam.X, -cam.Y) * AxisLength;
		}

		private static void DrawAxis(ImDrawListPtr draw, Vector2 origin, Vector2 dir, uint color, string? label)
		{
			Vector2 tip = origin + dir;
			draw.AddLine(origin, tip, color, LineThick);

			if (label is not null)
			{
				Vector2 textSize = ImGui.CalcTextSize(label);
				Vector2 labelPos = tip + Vector2.Normalize(dir) * 4f - textSize * 0.5f;
				draw.AddText(labelPos, color, label);
			}
		}
	}
}
