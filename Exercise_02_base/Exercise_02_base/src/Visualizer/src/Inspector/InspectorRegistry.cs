namespace Visualizer.Inspector;

public static class InspectorRegistry
{
    private static readonly Dictionary<Type, ComponentDrawer> _drawers = [];

    public static void Register<T>(ComponentDrawer<T> drawer) =>
        _drawers[typeof(T)] = drawer;

    public static bool TryGet(Type type, out ComponentDrawer? drawer) =>
        _drawers.TryGetValue(type, out drawer);
}
