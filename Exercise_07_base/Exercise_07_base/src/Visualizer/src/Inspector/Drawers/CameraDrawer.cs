using Core;
using ImGuiNET;
using Renderer.Objects;
using System.Numerics;

namespace Visualizer.Inspector.Drawers;

public sealed class CameraDrawer : ComponentDrawer<Camera>
{
    public override void DrawHeader(Camera target)
    {
        ImGui.TextDisabled("Camera");
        ImGui.Separator();
    }

    public override InspectorLayout BuildLayout(Camera target) => new()
    {
        Sections =
        [
            new()
            {
                Header = "Transform",
                Fields =
                [
                    new MemberField<Camera, Vector3>("Position",
                        c => c.Transforms.Position,
                        (c, v) => c.Transforms.Position = v),
                    new MemberField<Camera, Vector3>("Rotation",
                        c => EulerCache.GetEulerDegrees(c.Transforms),
                        (c, v) => EulerCache.SetEulerDegrees(c.Transforms, v)),
                ]
            },
            new()
            {
                Header = "Projection",
                Fields =
                [
                    new MemberField<Camera, float>("FOV (deg)",
                        c => Mathematics.RadiansToDegrees(c.FieldOfView),
                        (c, v) => c.FieldOfView = Mathematics.DegreesToRadians(v)),
                    new MemberField<Camera, float>("Near Plane",
                        c => c.NearPlane,
                        (c, v) => c.NearPlane = v),
                    new MemberField<Camera, float>("Far Plane",
                        c => c.FarPlane,
                        (c, v) => c.FarPlane = v),
                ]
            },
        ]
    };
}
