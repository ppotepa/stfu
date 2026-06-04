using STFU.NPR.Graph;

namespace STFU.NPR.Temporal;

public sealed record PreviousFrameGraph(
    IReadOnlyDictionary<int, PreviousFeatureCurve> CurvesByStableId,
    IReadOnlyDictionary<int, PreviousStroke> StrokesByStableId)
{
    public static PreviousFrameGraph Empty { get; } = new(
        new Dictionary<int, PreviousFeatureCurve>(),
        new Dictionary<int, PreviousStroke>());
}
