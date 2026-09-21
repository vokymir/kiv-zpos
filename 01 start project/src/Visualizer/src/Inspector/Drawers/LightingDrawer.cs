using ImGuiNET;
using Renderer.Objects;
using System.Numerics;

namespace Visualizer.Inspector.Drawers;

public sealed class LightingDrawer : ComponentDrawer<SceneLighting>
{
    public Action? OnModified { get; set; }

    public override void DrawHeader(SceneLighting target)
    {
        ImGui.TextDisabled($"Lighting  ({target.DirectionalLights.Count} directional)");
        ImGui.Separator();
    }

    public override InspectorLayout BuildLayout(SceneLighting target)
    {
        var layout = new InspectorLayout();

        layout.Sections.Add(new()
        {
            Header = "Ambient",
            Fields =
            [
                new MemberField<SceneLighting, Vector3>("Color",
                    l => l.Ambient.Color,
                    (l, v) => { l.Ambient.Color = v; OnModified?.Invoke(); }),
                new MemberField<SceneLighting, float>("Intensity",
                    l => l.Ambient.Intensity,
                    (l, v) => { l.Ambient.Intensity = v; OnModified?.Invoke(); }),
            ]
        });

        for (int i = 0; i < target.DirectionalLights.Count; i++)
        {
            int idx = i;
            layout.Sections.Add(new()
            {
                Header = $"Directional Light {idx + 1}",
                Fields =
                [
                    new MemberField<SceneLighting, Vector3>($"Color##{idx}",
                        l => l.DirectionalLights[idx].Color,
                        (l, v) => { l.DirectionalLights[idx].Color = v; OnModified?.Invoke(); }),
                    new MemberField<SceneLighting, float>($"Intensity##{idx}",
                        l => l.DirectionalLights[idx].Intensity,
                        (l, v) => { l.DirectionalLights[idx].Intensity = v; OnModified?.Invoke(); }),
                    new MemberField<SceneLighting, Vector3>($"Direction##{idx}",
                        l => l.DirectionalLights[idx].Direction,
                        (l, v) => { l.DirectionalLights[idx].Direction = v; OnModified?.Invoke(); }),
                ]
            });
        }

        return layout;
    }
}
