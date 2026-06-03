namespace STFU.Common.Primitives;

public readonly record struct EntityId(int Value)
{
    public static EntityId None { get; } = new(0);

    public bool IsNone => Value == 0;

    public override string ToString()
    {
        return Value.ToString();
    }
}
