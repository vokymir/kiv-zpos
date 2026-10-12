using Core.PostProcessing;
using ImGuiNET;
using Renderer.Components;
using Renderer.Objects;
using System.Numerics;
using Visualizer.Scripts;

namespace Visualizer.GUI
{
	internal sealed class InspectorPanel
	{
		public bool GenerateNormalsMetaChangedThisFrame { get; private set; }

		public void ResetFrameFlags() => GenerateNormalsMetaChangedThisFrame = false;

		public void Draw(
			object? selectedObject,
			SelectionController selection,
			Func<(AssetMetadata meta, string assetPath, string[] channels)?>? layoutDataProvider)
		{
			ImGuiViewportPtr viewport = ImGui.GetMainViewport();
			float topOffset  = ImGui.GetFrameHeight();
			Vector2 workPos  = viewport.WorkPos;
			Vector2 workSize = viewport.WorkSize;

			float rightWidth = workSize.X * 0.25f;
			ImGui.SetNextWindowPos(
				new Vector2(workPos.X + workSize.X - rightWidth, workPos.Y + topOffset),
				ImGuiCond.FirstUseEver);
			ImGui.SetNextWindowSize(
				new Vector2(rightWidth, workSize.Y - topOffset),
				ImGuiCond.FirstUseEver);

			ImGui.Begin("Inspector");

			if (selectedObject is null)
			{
				ImGui.TextDisabled("Nothing selected.");
				ImGui.End();
				return;
			}

			InspectorRenderer.DrawObject(selectedObject);

			if (selectedObject is Entity { Mesh: { } mesh })
				DrawMeshInfo(mesh, selection.SelectedVertexIndex, selection.SelectedFaceIndex);

			if (selectedObject is Entity)
				DrawImportSettings(layoutDataProvider);

			ImGui.End();
		}

		private void DrawImportSettings(Func<(AssetMetadata, string, string[])?>? layoutDataProvider)
		{
			var data = layoutDataProvider?.Invoke();
			if (data is null)
				return;

			var (meta, _, _) = data.Value;

			ImGui.Spacing();
			ImGui.Separator();
			ImGui.Text("Import Settings");
			ImGui.Separator();

			NormalsMode currentNormals = meta.Scene?.Model?.NormalsMode ?? NormalsMode.Flat;
			ImGui.SetNextItemWidth(120f);
			if (ImGui.BeginCombo("Normals", currentNormals.ToString()))
			{
				foreach (NormalsMode mode in Enum.GetValues<NormalsMode>())
				{
					bool selected = currentNormals == mode;
					if (ImGui.Selectable(mode.ToString(), selected) && !selected)
					{
						meta.EnsureModel().NormalsMode = mode;
						GenerateNormalsMetaChangedThisFrame = true;
					}
					if (selected)
						ImGui.SetItemDefaultFocus();
				}
				ImGui.EndCombo();
			}
		}

		private static void DrawMeshInfo(Mesh mesh, int? selectedVertex, int? selectedFace)
		{
			ImGui.Spacing();
			ImGui.Separator();
			ImGui.Text("Mesh Info");
			ImGui.Separator();

			int faceCount = mesh.Indices.Length / 3;
			ImGui.Text($"Vertices : {mesh.Vertices.Length}");
			ImGui.Text($"Faces    : {faceCount}");

			ImGui.Spacing();
			if (selectedVertex.HasValue)
				DrawVertexLabel(mesh, selectedVertex.Value);
			else if (selectedFace.HasValue)
				DrawFaceLabel(mesh, selectedFace.Value);
			else
				ImGui.TextDisabled("Right-click to select.");
		}

		private static void DrawFaceLabel(Mesh mesh, int faceIndex)
		{
			if (faceIndex < 0 || faceIndex * 3 + 2 >= mesh.Indices.Length)
				return;

			int i0 = mesh.Indices[faceIndex * 3];
			int i1 = mesh.Indices[faceIndex * 3 + 1];
			int i2 = mesh.Indices[faceIndex * 3 + 2];

			if (ImGui.TreeNodeEx($"Face {faceIndex}", ImGuiTreeNodeFlags.DefaultOpen))
			{
				ImGui.Text($"Indices  : {i0}, {i1}, {i2}");

				float[] c0 = mesh.Vertices[i0].Channels["position"];
				float[] c1 = mesh.Vertices[i1].Channels["position"];
				float[] c2 = mesh.Vertices[i2].Channels["position"];

				Vector3 p0 = new(c0[0], c0[1], c0[2]);
				Vector3 p1 = new(c1[0], c1[1], c1[2]);
				Vector3 p2 = new(c2[0], c2[1], c2[2]);
				Vector3 center = (p0 + p1 + p2) / 3f;
				Vector3 normal = Vector3.Normalize(Vector3.Cross(p1 - p0, p2 - p0));

				ImGui.Text($"Center   : ({center.X:F4}, {center.Y:F4}, {center.Z:F4})");
				ImGui.Text($"Normal   : ({normal.X:F4}, {normal.Y:F4}, {normal.Z:F4})");
				ImGui.TreePop();
			}
		}

		private static void DrawVertexLabel(Mesh mesh, int index)
		{
			if (mesh.Vertices.Length == 0 || index < 0 || index >= mesh.Vertices.Length)
				return;

			Core.Structures.Vertex v = mesh.Vertices[index];

			if (ImGui.TreeNodeEx($"Vertex {index}", ImGuiTreeNodeFlags.DefaultOpen))
			{
				foreach (var (name, data) in v.Channels)
					ImGui.Text($"{name,-12}: ({string.Join(", ", data.Select(f => f.ToString("F4")))})");
				ImGui.TreePop();
			}
		}
	}
}
