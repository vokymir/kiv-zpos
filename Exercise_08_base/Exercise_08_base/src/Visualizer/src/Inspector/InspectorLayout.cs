using ImGuiNET;

namespace Visualizer.Inspector;

public sealed class InspectorLayout
{
    public List<InspectorSection> Sections { get; init; } = [];
}

public sealed class InspectorSection
{
    // null = no collapsible header, always visible
    public string? Header { get; init; }
    public bool DefaultOpen { get; init; } = true;
    public List<IInspectorField> Fields { get; init; } = [];

    public ImGuiTreeNodeFlags TreeNodeFlags =>
        DefaultOpen ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None;
}
