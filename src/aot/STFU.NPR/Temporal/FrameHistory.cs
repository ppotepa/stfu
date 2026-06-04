using STFU.NPR.Pipeline;

namespace STFU.NPR.Temporal;

public sealed class FrameHistory
{
    public int PreviousFrameId { get; init; }

    public required NprViewContext PreviousView { get; init; }

    public IReadOnlyDictionary<int, PreviousFeatureCurve> CurvesByStableId { get; init; } =
        new Dictionary<int, PreviousFeatureCurve>();

    public IReadOnlyDictionary<int, PreviousStroke> StrokesByStableId { get; init; } =
        new Dictionary<int, PreviousStroke>();
}
