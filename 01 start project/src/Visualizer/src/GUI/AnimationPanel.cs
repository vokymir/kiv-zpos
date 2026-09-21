using ImGuiNET;
using Visualizer.Scripts;

namespace Visualizer.GUI
{
	internal sealed class AnimationPanel
	{
		public string? SelectedAnimation     { get; set; }
		public bool AnimationChangedThisFrame { get; private set; }
		public bool ApiDataMode { get; set; }

		public void ResetFrameFlags() => AnimationChangedThisFrame = false;

		public void Draw(MeshAnimationController? animController)
		{
			if (animController is null)
				return;

			ImGui.Begin("Animation");

			ImGui.SetNextItemWidth(220f);
			if (ApiDataMode)
			{
				ImGui.BeginDisabled();
				ImGui.BeginCombo("##AnimSeq", "API Data");
				ImGui.EndDisabled();
			}
			else
			{
				string preview = SelectedAnimation ?? "- Select animation -";
				if (ImGui.BeginCombo("##AnimSeq", preview))
				{
					if (ImGui.Selectable("(none)", SelectedAnimation is null))
					{
						SelectedAnimation = null;
						AnimationChangedThisFrame = true;
					}

					foreach (string seq in AnimationLibrary.DiscoverSequences())
					{
						bool sel = seq == SelectedAnimation;
						if (ImGui.Selectable(seq, sel))
						{
							SelectedAnimation = seq;
							AnimationChangedThisFrame = true;
						}
						if (sel) ImGui.SetItemDefaultFocus();
					}
					ImGui.EndCombo();
				}
			}

			if (!animController.HasAnimation)
			{
				ImGui.TextDisabled("No animation loaded.");
				ImGui.End();
				return;
			}

			ImGui.SameLine();
			ImGui.TextDisabled($"Frame {animController.CurrentFrameIndex + 1} / {animController.FrameCount}");

			ImGui.Spacing();

			string playLabel = animController.IsPlaying ? $"{Icons.Pause}##play" : $"{Icons.Play}##play";
			if (ImGui.Button(playLabel))
			{
				if (animController.IsPlaying) animController.Pause();
				else animController.Play();
			}
			ImGui.SameLine();

			if (ImGui.Button($"{Icons.Reset}##reset"))
				animController.Reset();
			ImGui.SameLine();

			bool isReverse = animController.IsReverse;
			if (isReverse)
				ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive]);
			if (ImGui.Button($"{Icons.Reverse}##reverse"))
				animController.IsReverse = !animController.IsReverse;
			if (isReverse)
				ImGui.PopStyleColor();
			ImGui.SameLine();

			ImGui.SetNextItemWidth(120f);
			float speed = animController.Speed;
			if (ImGui.SliderFloat("Speed##anim", ref speed, 0.1f, 5.0f, "%.1fx"))
				animController.Speed = speed;
			ImGui.SameLine();

			ImGui.SetNextItemWidth(100f);
			float fps = animController.FPS;
			if (ImGui.SliderFloat("FPS##anim", ref fps, 1f, 60f, "%.0f"))
				animController.FPS = fps;

			ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
			int frameIdx = animController.CurrentFrameIndex;
			int maxFrame = animController.FrameCount - 1;
			if (ImGui.SliderInt("##scrubber", ref frameIdx, 0, maxFrame))
				animController.SeekTo(frameIdx);

			ImGui.End();
		}
	}
}
