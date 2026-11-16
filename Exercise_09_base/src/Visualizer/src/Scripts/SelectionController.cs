using ImGuiNET;
using Renderer.Components;
using Renderer.Objects;
using System.Numerics;

namespace Visualizer.Scripts
{
	internal sealed class SelectionController
	{
		public int? SelectedVertexIndex { get; private set; }
		public int? SelectedFaceIndex   { get; private set; }

		private const float PickRadiusPx = 12f;

		public void ResetSelection()
		{
			SelectedVertexIndex = null;
			SelectedFaceIndex   = null;
		}

		/// <summary>Restores a previously saved selection. Indices are applied as-is; the caller must validate against mesh bounds.</summary>
		public void Restore(int? vertexIndex, int? faceIndex)
		{
			SelectedVertexIndex = vertexIndex;
			SelectedFaceIndex   = faceIndex;
		}

		/// <summary>
		/// Clears the current selection if it no longer fits within <paramref name="mesh"/>.
		/// Needed because animation frames can have different vertex/face counts.
		/// </summary>
		public void ClampToMesh(Mesh? mesh)
		{
			if (SelectedVertexIndex.HasValue &&
			    (mesh is null || SelectedVertexIndex.Value < 0 || SelectedVertexIndex.Value >= mesh.Vertices.Length))
				SelectedVertexIndex = null;

			if (SelectedFaceIndex.HasValue &&
			    (mesh is null || SelectedFaceIndex.Value < 0 || SelectedFaceIndex.Value * 3 + 2 >= mesh.Indices.Length))
				SelectedFaceIndex = null;
		}

		public void TryPick(
			SelectionMode mode,
			Vector2 mousePos,
			Vector2 imageOrigin,
			Vector2 viewportSize,
			Camera camera,
			Entity entity)
		{
			if (mode == SelectionMode.Vertex)
				TryPickVertex(mousePos, imageOrigin, viewportSize, camera, entity);
			else
				TryPickFace(mousePos, imageOrigin, viewportSize, camera, entity);
		}

		public void DrawOverlays(Vector2 imageOrigin, Vector2 viewportSize, Camera camera, Entity entity)
		{
			Mesh? mesh = entity.Mesh;
			if (mesh is null)
				return;

			if (SelectedVertexIndex.HasValue && mesh.Vertices.Length > 0)
				DrawVertexOverlay(mesh, SelectedVertexIndex.Value, imageOrigin, viewportSize, camera, entity);

			if (SelectedFaceIndex.HasValue)
				DrawFaceOverlay(mesh, SelectedFaceIndex.Value, imageOrigin, viewportSize, camera, entity);
		}

		private void TryPickVertex(Vector2 mousePos, Vector2 imageOrigin, Vector2 viewportSize, Camera camera, Entity entity)
		{
			Mesh? mesh = entity.Mesh;
			if (mesh is null || mesh.Vertices.Length == 0)
				return;

			Vector2 mouseLocal = mousePos - imageOrigin;
			float   ndcX       =  mouseLocal.X / viewportSize.X * 2f - 1f;
			float   ndcY       = -(mouseLocal.Y / viewportSize.Y * 2f - 1f);

			float aspect = viewportSize.X / viewportSize.Y;
			Ray   ray    = Ray.FromNdc(ndcX, ndcY, camera, aspect);

			Matrix4x4 model        = entity.Transforms.Matrix;
			Matrix4x4.Invert(model, out Matrix4x4 invModel);
			Matrix4x4 normalMatrix = Matrix4x4.Transpose(invModel);
			Vector3   cameraPos    = camera.Transforms.Position;

			float halfFov        = camera.FieldOfView * 0.5f;
			float halfHeight     = viewportSize.Y * 0.5f;
			float radiusPerDepth = MathF.Tan(halfFov) * (PickRadiusPx / halfHeight);

			var vertices = mesh.Vertices;
			int bestIndex = -1;
			float bestT   = float.MaxValue;
			object sync   = new();

			Parallel.For(
				0, vertices.Length,
				() => (-1, float.MaxValue),
				(i, _, local) =>
				{
					float[] pos  = vertices[i].Channels["position"];
					float[] norm = vertices[i].Channel("normal");

					Vector3 worldPos  = Vector3.Transform(new Vector3(pos[0], pos[1], pos[2]), model);
					Vector3 worldNorm = Vector3.Normalize(
						Vector3.TransformNormal(new Vector3(norm.Length > 2 ? norm[0] : 0f, norm.Length > 2 ? norm[1] : 1f, norm.Length > 2 ? norm[2] : 0f), normalMatrix));

					if (Vector3.Dot(worldNorm, cameraPos - worldPos) <= 0f)
						return local;

					float t = ray.ClosestT(worldPos);
					if (t <= 0f || t >= local.Item2)
						return local;

					float worldRadius = t * radiusPerDepth;
					if (ray.DistanceTo(worldPos) > worldRadius)
						return local;

					return (i, t);
				},
				local =>
				{
					if (local.Item1 < 0) return;
					lock (sync)
					{
						if (local.Item2 < bestT)
						{
							bestT     = local.Item2;
							bestIndex = local.Item1;
						}
					}
				});

			SelectedVertexIndex = bestIndex >= 0 ? bestIndex : null;
		}

		private void TryPickFace(Vector2 mousePos, Vector2 imageOrigin, Vector2 viewportSize, Camera camera, Entity entity)
		{
			Mesh? mesh = entity.Mesh;
			if (mesh is null || mesh.Indices.Length < 3)
				return;

			Vector2 mouseLocal = mousePos - imageOrigin;
			float   ndcX       =  mouseLocal.X / viewportSize.X * 2f - 1f;
			float   ndcY       = -(mouseLocal.Y / viewportSize.Y * 2f - 1f);

			float aspect = viewportSize.X / viewportSize.Y;
			Ray   ray    = Ray.FromNdc(ndcX, ndcY, camera, aspect);

			Matrix4x4 model = entity.Transforms.Matrix;

			var vertices = mesh.Vertices;
			var indices  = mesh.Indices;
			int faceCount = indices.Length / 3;
			int  bestFace = -1;
			float bestT   = float.MaxValue;
			object sync   = new();

			Parallel.For(
				0, faceCount,
				() => (-1, float.MaxValue),
				(f, _, local) =>
				{
					int i0 = indices[f * 3];
					int i1 = indices[f * 3 + 1];
					int i2 = indices[f * 3 + 2];

					float[] c0 = vertices[i0].Channels["position"];
					float[] c1 = vertices[i1].Channels["position"];
					float[] c2 = vertices[i2].Channels["position"];

					Vector3 p0 = Vector3.Transform(new Vector3(c0[0], c0[1], c0[2]), model);
					Vector3 p1 = Vector3.Transform(new Vector3(c1[0], c1[1], c1[2]), model);
					Vector3 p2 = Vector3.Transform(new Vector3(c2[0], c2[1], c2[2]), model);

					Vector3 faceNormal = Vector3.Normalize(Vector3.Cross(p1 - p0, p2 - p0));
					if (Vector3.Dot(faceNormal, -ray.Direction) <= 0f)
						return local;

					const float Epsilon = 1e-7f;
					Vector3 edge1 = p1 - p0;
					Vector3 edge2 = p2 - p0;
					Vector3 h     = Vector3.Cross(ray.Direction, edge2);
					float   a     = Vector3.Dot(edge1, h);
					if (MathF.Abs(a) < Epsilon)
						return local;

					float   f2 = 1f / a;
					Vector3 s  = ray.Origin - p0;
					float   u  = f2 * Vector3.Dot(s, h);
					if (u < 0f || u > 1f)
						return local;

					Vector3 q = Vector3.Cross(s, edge1);
					float   v = f2 * Vector3.Dot(ray.Direction, q);
					if (v < 0f || u + v > 1f)
						return local;

					float t = f2 * Vector3.Dot(edge2, q);
					if (t > Epsilon && t < local.Item2)
						return (f, t);

					return local;
				},
				local =>
				{
					if (local.Item1 < 0) return;
					lock (sync)
					{
						if (local.Item2 < bestT)
						{
							bestT    = local.Item2;
							bestFace = local.Item1;
						}
					}
				});

			SelectedFaceIndex = bestFace >= 0 ? bestFace : null;
		}

		private void DrawVertexOverlay(Mesh mesh, int vertexIndex, Vector2 imageOrigin, Vector2 viewportSize, Camera camera, Entity entity)
		{
			Core.Structures.Vertex v = mesh.Vertices[vertexIndex];
			float[] pos = v.Channels["position"];
			Vector3 worldPos = Vector3.Transform(new Vector3(pos[0], pos[1], pos[2]), entity.Transforms.Matrix);

			if (!TryWorldToScreen(worldPos, viewportSize, camera, out Vector2 screenPos))
				return;

			ImDrawListPtr draw     = ImGui.GetWindowDrawList();
			Vector2       labelPos = imageOrigin + screenPos + new Vector2(6f, -8f);

			List<(string text, bool dim)> lines = [($"Vertex {vertexIndex}", false)];
			foreach (var (name, data) in v.Channels)
				lines.Add(($"{name}: ({string.Join(", ", data.Select(f => f.ToString("F3")))})", true));

			DrawLabelBox(draw, labelPos, lines, ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 0.3f)));

			draw.AddCircleFilled(imageOrigin + screenPos, 3f,
				ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.8f, 0.2f, 1f)));
		}

		private void DrawFaceOverlay(Mesh mesh, int faceIndex, Vector2 imageOrigin, Vector2 viewportSize, Camera camera, Entity entity)
		{
			int i0 = mesh.Indices[faceIndex * 3];
			int i1 = mesh.Indices[faceIndex * 3 + 1];
			int i2 = mesh.Indices[faceIndex * 3 + 2];

			float[] q0 = mesh.Vertices[i0].Channels["position"];
			float[] q1 = mesh.Vertices[i1].Channels["position"];
			float[] q2 = mesh.Vertices[i2].Channels["position"];

			Matrix4x4 model = entity.Transforms.Matrix;
			Vector3 p0 = Vector3.Transform(new Vector3(q0[0], q0[1], q0[2]), model);
			Vector3 p1 = Vector3.Transform(new Vector3(q1[0], q1[1], q1[2]), model);
			Vector3 p2 = Vector3.Transform(new Vector3(q2[0], q2[1], q2[2]), model);

			bool ok0 = TryWorldToScreen(p0, viewportSize, camera, out Vector2 s0);
			bool ok1 = TryWorldToScreen(p1, viewportSize, camera, out Vector2 s1);
			bool ok2 = TryWorldToScreen(p2, viewportSize, camera, out Vector2 s2);

			ImDrawListPtr draw   = ImGui.GetWindowDrawList();
			uint          orange = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.55f, 0.1f, 1f));
			uint          fill   = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.55f, 0.1f, 0.15f));

			if (ok0 && ok1 && ok2)
			{
				Vector2 a = imageOrigin + s0;
				Vector2 b = imageOrigin + s1;
				Vector2 c = imageOrigin + s2;

				draw.AddTriangleFilled(a, b, c, fill);
				draw.AddTriangle(a, b, c, orange, 1.5f);
				draw.AddCircleFilled(a, 3f, orange);
				draw.AddCircleFilled(b, 3f, orange);
				draw.AddCircleFilled(c, 3f, orange);
			}

			if (!ok0 && !ok1 && !ok2)
				return;

			// Place label outside the triangle's screen-space bounding box
			float bbMinX = float.MaxValue, bbMinY = float.MaxValue;
			float bbMaxX = float.MinValue, bbMaxY = float.MinValue;
			if (ok0) { bbMinX = MathF.Min(bbMinX, s0.X); bbMaxX = MathF.Max(bbMaxX, s0.X); bbMinY = MathF.Min(bbMinY, s0.Y); bbMaxY = MathF.Max(bbMaxY, s0.Y); }
			if (ok1) { bbMinX = MathF.Min(bbMinX, s1.X); bbMaxX = MathF.Max(bbMaxX, s1.X); bbMinY = MathF.Min(bbMinY, s1.Y); bbMaxY = MathF.Max(bbMaxY, s1.Y); }
			if (ok2) { bbMinX = MathF.Min(bbMinX, s2.X); bbMaxX = MathF.Max(bbMaxX, s2.X); bbMinY = MathF.Min(bbMinY, s2.Y); bbMaxY = MathF.Max(bbMaxY, s2.Y); }

			float bbCenterY = (bbMinY + bbMaxY) * 0.5f;
			Vector2 labelPos = imageOrigin + new Vector2(bbMaxX + 10f, bbCenterY - ImGui.GetTextLineHeight() * 1.5f);
			Vector3 normal   = Vector3.Normalize(Vector3.Cross(p1 - p0, p2 - p0));

			List<(string text, bool dim)> lines = [
				($"Face {faceIndex}", false),
				($"v: {i0}, {i1}, {i2}", true),
				($"N ({normal.X:F3}, {normal.Y:F3}, {normal.Z:F3})", true),
			];

			DrawLabelBox(draw, labelPos, lines, ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.55f, 0.1f, 0.6f)));
		}

		/// <summary>Draws a translucent background box with a border and the given lines of text, anchored at <paramref name="pos"/>.</summary>
		private static void DrawLabelBox(ImDrawListPtr draw, Vector2 pos, IReadOnlyList<(string text, bool dim)> lines, uint borderColor)
		{
			uint bg    = ImGui.ColorConvertFloat4ToU32(new Vector4(0f,    0f,    0f,    0.6f));
			uint white = ImGui.ColorConvertFloat4ToU32(new Vector4(1f,    1f,    1f,    1f));
			uint dim   = ImGui.ColorConvertFloat4ToU32(new Vector4(0.75f, 0.75f, 0.75f, 1f));

			float pad      = 4f;
			float lineH    = ImGui.GetTextLineHeight();
			float boxW     = lines.Max(l => ImGui.CalcTextSize(l.text).X);
			float boxH     = pad * 2f + lineH * lines.Count + 2f;
			Vector2 boxMin = pos - new Vector2(pad, pad);
			Vector2 boxMax = boxMin + new Vector2(boxW + pad * 2f, boxH);

			draw.AddRectFilled(boxMin, boxMax, bg, 3f);
			draw.AddRect(boxMin, boxMax, borderColor, 3f);
			for (int i = 0; i < lines.Count; i++)
				draw.AddText(pos + new Vector2(0f, lineH * i), lines[i].dim ? dim : white, lines[i].text);
		}

		private static bool TryWorldToScreen(Vector3 worldPos, Vector2 viewportSize, Camera camera, out Vector2 screenPos)
		{
			screenPos = default;

			float aspect = viewportSize.X / viewportSize.Y;
			Matrix4x4 vp = camera.GetViewMatrix() * camera.GetProjectionMatrix(aspect);

			Vector4 clip = Vector4.Transform(new Vector4(worldPos, 1f), vp);
			if (clip.W <= 0f)
				return false;

			Vector3 ndc = new Vector3(clip.X, clip.Y, clip.Z) / clip.W;
			if (ndc.X < -1f || ndc.X > 1f || ndc.Y < -1f || ndc.Y > 1f)
				return false;

			screenPos = new Vector2(
				(ndc.X * 0.5f + 0.5f) * viewportSize.X,
				(1f - (ndc.Y * 0.5f + 0.5f)) * viewportSize.Y);

			return true;
		}
	}
}
