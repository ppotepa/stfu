namespace STFU.NPR.Graph;

public readonly record struct StrokeStableId(int Value)
{
    public static implicit operator int(StrokeStableId value) => value.Value;
    public static implicit operator StrokeStableId(int value) => new(value);
}
