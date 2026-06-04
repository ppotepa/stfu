namespace STFU.NPR.Graph;

public readonly record struct CurveParameterRange(float Start, float End)
{
    public static CurveParameterRange Normalized { get; } = new(0f, 1f);

    public float Length => End - Start;
}
