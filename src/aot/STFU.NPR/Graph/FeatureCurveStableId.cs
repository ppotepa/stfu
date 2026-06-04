namespace STFU.NPR.Graph;

public readonly record struct FeatureCurveStableId(int Value)
{
    public static implicit operator int(FeatureCurveStableId value) => value.Value;
    public static implicit operator FeatureCurveStableId(int value) => new(value);
}
