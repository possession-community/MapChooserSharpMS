namespace MapChooserSharpMS.Shared.Events;

public readonly struct McsValueOverrideEvent<T>
{
    public static McsValueOverrideEvent<T> NoOverride => default;

    public bool HasValue { get; }
    public T Value { get; }

    public McsValueOverrideEvent(T value)
    {
        HasValue = value is not null;
        Value = value;
    }
}
