using STFU.NPR.Graph;
using STFU.NPR.Pipeline;
using STFU.Strokes;

namespace STFU.NPR.Temporal;

public sealed class FrameHistoryState
{
    private readonly Dictionary<int, List<VisibilitySegment>> _segmentGroups = [];
    private readonly Dictionary<int, StrokePath2D> _pathsByStableId = [];

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
        _segmentGroups.Clear();
        for (var i = 0; i < graph.VisibilitySegments.Count; i++)
        {
            var segment = graph.VisibilitySegments[i];
            if (!_segmentGroups.TryGetValue(segment.FeatureCurveId, out var list))
            {
                list = [];
                _segmentGroups.Add(segment.FeatureCurveId, list);
            }

            list.Add(segment);
        }

        for (var i = 0; i < graph.Curves.Count; i++)
        {
            var curve = graph.Curves[i];
            curves[curve.StableId] = new PreviousFeatureCurve(
                curve.StableId,
                curve.Kind,
                curve.Source,
                curve.Points.ToArray(),
                _segmentGroups.TryGetValue(curve.StableId, out var segments)
                    ? segments.ToArray()
                    : Array.Empty<VisibilitySegment>(),
                graph.GetSalience(curve.StableId, curve.Importance));
        }

        _pathsByStableId.Clear();
        for (var i = 0; i < frame.Paths.Count; i++)
        {
            var path = frame.Paths[i];
            if (path.Metadata is not StrokeMetadata metadata)
            {
                continue;
            }

            if (!_pathsByStableId.ContainsKey(metadata.StableId))
            {
                _pathsByStableId.Add(metadata.StableId, path);
            }
        }

        var strokes = new Dictionary<int, PreviousStroke>(graph.StyledStrokes.Count);
        for (var i = 0; i < graph.StyledStrokes.Count; i++)
        {
            var styled = graph.StyledStrokes[i];
            if (!_pathsByStableId.TryGetValue(styled.StableId, out var path))
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
