namespace Visualizer.Inspector;

public sealed class MemberField<TTarget, TValue> : IInspectorField
{
    private readonly Func<TTarget, TValue> _getter;
    private readonly Action<TTarget, TValue>? _setter;

    public string Label { get; }
    public bool ReadOnly => _setter is null;
    public Type ValueType => typeof(TValue);

    public MemberField(string label, Func<TTarget, TValue> getter, Action<TTarget, TValue>? setter = null)
    {
        Label = label;
        _getter = getter;
        _setter = setter;
    }

    public object? Get(object target) => _getter((TTarget)target);

    public void Set(object target, object? value)
    {
        if (_setter is not null && value is TValue typed)
            _setter((TTarget)target, typed);
    }
}
