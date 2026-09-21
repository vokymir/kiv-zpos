using Core.Structures;
using Renderer.Wrappers.Model;

namespace Visualizer
{
	internal interface IVisualizerCommand { }

	internal sealed record SetMeshCommand(Vertex[] Vertices, int[] Indices, List<VertexChannelSpec>? Layout, string? SceneName) : IVisualizerCommand;

	internal sealed record FrameData(Vertex[] Vertices, int[] Indices, List<VertexChannelSpec>? Layout);
	internal sealed record SetAnimationCommand(FrameData[] Frames, string? SceneName) : IVisualizerCommand;

	internal sealed record PlayCommand  : IVisualizerCommand;
	internal sealed record PauseCommand : IVisualizerCommand;
	internal sealed record StopCommand  : IVisualizerCommand;
}
