namespace STFU.Common.Primitives;

public readonly record struct AnimationClipHandle(int Value)
{
    public static AnimationClipHandle None { get; } = new(0);

    public bool IsNone => Value == 0;

    public override string ToString()
    {
        return Value.ToString();
    }
}
