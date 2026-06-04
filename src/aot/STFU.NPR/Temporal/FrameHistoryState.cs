using STFU.NPR.Graph;
using STFU.NPR.Pipeline;
using STFU.Strokes;

namespace STFU.NPR.Temporal;

public sealed class FrameHistoryState
{
    public int LastFrameId => Latest?.PreviousFrameId ?? 0;

    public FrameHistory? Latest { get; private set; }

    public int PeekNextFrameId()
    {
        return LastFrameId + 1;
    }

    public FrameHistory? GetPreviousFrame()
    {
        return Latest;
    }

    public void Reset()
    {
        Latest = null;
    }

    public void Capture(NprViewContext currentView, NprGraph graph, StrokeFrame frame, float timeSeconds)
    {
        var previous = Latest;
        var curves = new Dictionary<int, PreviousFeatureCurve>(graph.Curves.Count);
        var groupedSegments = graph.VisibilitySegments
            .GroupBy(segment => segment.FeatureCurveId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<VisibilitySegment>)group.ToArray());

        foreach (var curve in graph.Curves)
        {
            curves[curve.StableId] = new PreviousFeatureCurve(
                curve.StableId,
                curve.Kind,
                curve.Source,
                curve.Points.ToArray(),
                groupedSegments.GetValueOrDefault(curve.StableId, Array.Empty<VisibilitySegment>()),
                graph.GetSalience(curve.StableId, curve.Importance));
        }

        var pathsByStableId = frame.Paths
            .Where(path => path.Metadata is not null)
            .GroupBy(path => path.Metadata!.Value.StableId)
            .ToDictionary(group => group.Key, group => group.First());

        var strokes = new Dictionary<int, PreviousStroke>(graph.StyledStrokes.Count);
        foreach (var styled in graph.StyledStrokes)
        {
            if (!pathsByStableId.TryGetValue(styled.StableId, out var path))
            {
                continue;
            }

            var state = previous is not null && previous.StrokesByStableId.ContainsKey(styled.StableId)
                ? TemporalStrokeState.Alive
                : TemporalStrokeState.FadingIn;
            var lifetime = previous is not null && previous.StrokesByStableId.TryGetValue(styled.StableId, out var prior)
                ? prior.Lifetime + 1f
                : 1f;

            strokes[styled.StableId] = new PreviousStroke(
                styled.StableId,
                styled.FeatureCurveId,
                styled.Intent,
                path,
                lifetime,
                timeSeconds,
                state);
        }

        Latest = new FrameHistory
        {
            PreviousFrameId = currentView.FrameId,
            PreviousView = currentView.WithoutHistory(),
            CurvesByStableId = curves,
            StrokesByStableId = strokes
        };
    }
}
