using ImGuiNET;
using Renderer.Components;
using Renderer.Objects;
using System.Numerics;

namespace Visualizer.Inspector.Drawers;

public sealed class EntityDrawer : ComponentDrawer<Entity>
{
    public Action? OnModified { get; set; }

    public override void DrawHeader(Entity target)
    {
        string meshInfo = target.Mesh is { } m
            ? $"{m.Vertices.Length} verts  {m.Indices.Length / 3} tris"
            : "No mesh";

        ImGui.TextDisabled(meshInfo);
        ImGui.Separator();
    }

    public override InspectorLayout BuildLayout(Entity target) => new()
    {
        Sections =
        [
            new()
            {
                Header = "Transform",
                Fields =
                [
                    new MemberField<Entity, Vector3>("Position",
                        e => e.Transforms.Position,
                        (e, v) => { e.Transforms.Position = v; OnModified?.Invoke(); }),
                    new MemberField<Entity, Vector3>("Rotation",
                        e => EulerCache.GetEulerDegrees(e.Transforms),
                        (e, v) => { EulerCache.SetEulerDegrees(e.Transforms, v); OnModified?.Invoke(); }),
                    new MemberField<Entity, Vector3>("Scale",
                        e => e.Transforms.Scale,
                        (e, v) => { e.Transforms.Scale = v; OnModified?.Invoke(); }),
                ]
            },
            new()
            {
                Header = "Material",
                Fields =
                [
                    new MemberField<Entity, Vector3>("Tint",
                        e =>
                        {
                            Vector3 tint = MaterialParams.DefaultTint;
                            e.Material?.TryGetParameter(MaterialParams.Tint, out tint);
                            return tint;
                        },
                        (e, v) => { e.Material?.SetParameter(MaterialParams.Tint, v); OnModified?.Invoke(); }),
                    new MemberField<Entity, Vector3>("Specular Color",
                        e =>
                        {
                            Vector3 v = MaterialParams.DefaultSpecularColor;
                            e.Material?.TryGetParameter(MaterialParams.SpecularColor, out v);
                            return v;
                        },
                        (e, v) => { e.Material?.SetParameter(MaterialParams.SpecularColor, v); OnModified?.Invoke(); }),
                    new MemberField<Entity, float>("Specular Strength",
                        e =>
                        {
                            float v = MaterialParams.DefaultSpecularStrength;
                            e.Material?.TryGetParameter(MaterialParams.SpecularStrength, out v);
                            return v;
                        },
                        (e, v) => { e.Material?.SetParameter(MaterialParams.SpecularStrength, v); OnModified?.Invoke(); }),
                    new MemberField<Entity, float>("Shininess",
                        e =>
                        {
                            float v = MaterialParams.DefaultShininess;
                            e.Material?.TryGetParameter(MaterialParams.Shininess, out v);
                            return v;
                        },
                        (e, v) => { e.Material?.SetParameter(MaterialParams.Shininess, v); OnModified?.Invoke(); }),
                ]
            },
        ]
    };
}
