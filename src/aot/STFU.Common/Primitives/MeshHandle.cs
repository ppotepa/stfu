namespace STFU.Common.Primitives;

public readonly record struct MeshHandle(int Value)
{
    public static MeshHandle None { get; } = new(0);

    public bool IsNone => Value == 0;

    public override string ToString()
    {
        return Value.ToString();
    }
}
