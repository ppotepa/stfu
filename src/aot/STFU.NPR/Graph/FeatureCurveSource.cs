namespace STFU.NPR.Graph;

public readonly record struct FeatureCurveSource(
    int StartVertexIndex,
    int EndVertexIndex,
    int FirstTriangleIndex,
    int SecondTriangleIndex)
{
    public static FeatureCurveSource None { get; } = new(-1, -1, -1, -1);
}
