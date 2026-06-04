using STFU.NPR.Graph;
using STFU.NPR.Pipeline;
using STFU.Strokes;

namespace STFU.NPR.Visibility;

public sealed class OfflineExactVisibilityResolver : IVisibilityResolver
{
    private const float SampleSpacingPixels = 2f;
    private const float TransitionMinPixels = 0.5f;
    private const int TransitionMaxDepth = 12;

    public IReadOnlyList<VisibilitySegment> Resolve(NprContext context, IReadOnlyList<FeatureCurve> curves)
    {
        var segments = new List<VisibilitySegment>(curves.Count * 3);

        foreach (var curve in curves)
        {
            if (curve.Points.Count < 2)
            {
                continue;
            }

            ResolveCurve(context, curve, segments);
        }

        return segments;
    }

    private static void ResolveCurve(NprContext context, FeatureCurve curve, List<VisibilitySegment> segments)
    {
        var line = curve.ToFeatureLine();
        if (curve.Intent == NprStrokeIntent.Silhouette)
        {
            segments.Add(CreateSegment(curve, 0, VisibilityState.Visible, 0f, 1f, line.Start, line.End, line.Depth));
            return;
        }

        var lineLength = MeasureLength(line.Start, line.End);
        var sampleCount = Math.Max(5, (int)MathF.Ceiling(lineLength / SampleSpacingPixels) + 1);
        var sampleTs = new float[sampleCount];
        var samplePoints = new Point2D[sampleCount];
        var sampleStates = new VisibilityState[sampleCount];

        for (var index = 0; index < sampleCount; index++)
        {
            var t = index / (float)(sampleCount - 1);
            var point = Lerp(line.Start, line.End, t);
            sampleTs[index] = t;
            samplePoints[index] = point;
            sampleStates[index] = QueryVisibility(context, point, line.Depth);
        }

        var pendingState = VisibilityState.Visible;
        var hasPending = false;
        var pendingStartT = 0f;
        var pendingStartPoint = line.Start;
        var pendingEndT = 0f;
        var pendingEndPoint = line.Start;
        var segmentIndex = 0;

        for (var index = 0; index < sampleCount - 1; index++)
        {
            var start = samplePoints[index];
            var end = samplePoints[index + 1];
            var startT = sampleTs[index];
            var endT = sampleTs[index + 1];
            var startState = sampleStates[index];
            var endState = sampleStates[index + 1];

            if (!hasPending)
            {
                pendingState = startState;
                pendingStartT = startT;
                pendingStartPoint = start;
                pendingEndT = endT;
                pendingEndPoint = end;
                hasPending = true;
            }
            else if (startState == pendingState)
            {
                pendingEndT = startT;
                pendingEndPoint = start;
            }
            else
            {
                segments.Add(CreateSegment(curve, segmentIndex++, pendingState, pendingStartT, pendingEndT, pendingStartPoint, pendingEndPoint, line.Depth));
                pendingState = startState;
                pendingStartT = startT;
                pendingStartPoint = start;
                pendingEndT = endT;
                pendingEndPoint = end;
            }

            if (startState == endState)
            {
                pendingEndT = endT;
                pendingEndPoint = end;
                continue;
            }

            var transition = RefineTransition(context, line.Depth, startT, endT, start, end, startState, endState, 0);

            if (pendingState == startState)
            {
                pendingEndT = transition.T;
                pendingEndPoint = transition.Point;
            }

            segments.Add(CreateSegment(curve, segmentIndex++, pendingState, pendingStartT, pendingEndT, pendingStartPoint, pendingEndPoint, line.Depth));
            pendingState = endState;
            pendingStartT = transition.T;
            pendingStartPoint = transition.Point;
            pendingEndT = endT;
            pendingEndPoint = end;
        }

        if (hasPending)
        {
            segments.Add(CreateSegment(curve, segmentIndex, pendingState, pendingStartT, pendingEndT, pendingStartPoint, pendingEndPoint, line.Depth));
        }
    }

    private static VisibilitySegment CreateSegment(
        FeatureCurve curve,
        int segmentIndex,
        VisibilityState state,
        float startT,
        float endT,
        Point2D start,
        Point2D end,
        float depth)
    {
        return new VisibilitySegment(
            HashSegmentId(curve.StableId, segmentIndex),
            curve.StableId,
            curve.Kind,
            curve.Intent,
            state,
            startT,
            endT,
            start,
            end,
            depth,
            curve.Shade,
            curve.Importance,
            curve.Confidence,
            curve.HatchLayerKind);
    }

    private static (float T, Point2D Point) RefineTransition(
        NprContext context,
        float lineDepth,
        float startT,
        float endT,
        Point2D start,
        Point2D end,
        VisibilityState startState,
        VisibilityState endState,
        int depth)
    {
        var length = MeasureLength(start, end);
        if (depth >= TransitionMaxDepth || length <= TransitionMinPixels)
        {
            var midT = (startT + endT) * 0.5f;
            return (midT, Lerp(start, end, 0.5f));
        }

        var midpoint = Lerp(start, end, 0.5f);
        var midTRefined = (startT + endT) * 0.5f;
        var midpointState = QueryVisibility(context, midpoint, lineDepth);

        if (midpointState == startState && midpointState != endState)
        {
            return RefineTransition(context, lineDepth, midTRefined, endT, midpoint, end, midpointState, endState, depth + 1);
        }

        if (midpointState == endState && midpointState != startState)
        {
            return RefineTransition(context, lineDepth, startT, midTRefined, start, midpoint, startState, midpointState, depth + 1);
        }

        return (midTRefined, midpoint);
    }

    private static VisibilityState QueryVisibility(NprContext context, Point2D point, float lineDepth)
    {
        return context.OcclusionQuery.IsOccluded(context, point, lineDepth)
            ? VisibilityState.Hidden
            : VisibilityState.Visible;
    }

    private static Point2D Lerp(Point2D start, Point2D end, float t)
    {
        return new Point2D(
            start.X + (end.X - start.X) * t,
            start.Y + (end.Y - start.Y) * t);
    }

    private static float MeasureLength(Point2D start, Point2D end)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private static int HashSegmentId(int curveStableId, int segmentIndex)
    {
        unchecked
        {
            return (curveStableId * 677) ^ (segmentIndex + 1);
        }
    }
}
