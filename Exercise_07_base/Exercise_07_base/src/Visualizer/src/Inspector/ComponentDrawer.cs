using ImGuiNET;

namespace Visualizer.Inspector;

public abstract class ComponentDrawer
{
    public abstract InspectorLayout BuildLayout(object target);

    // Override to draw custom content above the sections (e.g. a name field, icon, stats bar)
    public virtual void DrawHeader(object target) { }
}

public abstract class ComponentDrawer<T> : ComponentDrawer
{
    public override InspectorLayout BuildLayout(object target) => BuildLayout((T)target);
    public override void DrawHeader(object target) => DrawHeader((T)target);

    public abstract InspectorLayout BuildLayout(T target);
    public virtual void DrawHeader(T target) { }
}
