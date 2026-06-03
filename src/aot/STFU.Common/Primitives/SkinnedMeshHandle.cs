namespace STFU.Common.Primitives;

public readonly record struct SkinnedMeshHandle(int Value)
{
    public static SkinnedMeshHandle None { get; } = new(0);

    public bool IsNone => Value == 0;

    public override string ToString()
    {
        return Value.ToString();
    }
}
