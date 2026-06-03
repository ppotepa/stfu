namespace STFU.Common.Primitives;

public readonly record struct SkeletonHandle(int Value)
{
    public static SkeletonHandle None { get; } = new(0);

    public bool IsNone => Value == 0;

    public override string ToString()
    {
        return Value.ToString();
    }
}
