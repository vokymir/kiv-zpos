using ImGuiNET;

namespace Visualizer.GUI
{
	internal static class DockSpacePanel
	{
		public static void Draw()
		{
			ImGuiIOPtr io = ImGui.GetIO();
			if ((io.ConfigFlags & ImGuiConfigFlags.DockingEnable) == 0)
				return;

			uint dockspaceId = ImGui.GetID("MainDockSpace");
			ImGui.DockSpaceOverViewport(dockspaceId, ImGui.GetMainViewport(), ImGuiDockNodeFlags.PassthruCentralNode);
		}
	}
}
