namespace Visualizer.Inspector;

public interface IInspectorField
{
    string Label { get; }
    bool ReadOnly { get; }
    Type ValueType { get; }
    object? Get(object target);
    void Set(object target, object? value);
}
