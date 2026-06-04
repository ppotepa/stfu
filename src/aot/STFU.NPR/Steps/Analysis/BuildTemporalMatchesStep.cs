using STFU.NPR.Pipeline;
using STFU.NPR.Temporal;
using STFU.NPR.Graph;
using STFU.Strokes;

namespace STFU.NPR.Steps.Analysis;

public sealed class BuildTemporalMatchesStep : INprStep
{
    public void Execute(NprContext context)
    {
        context.Graph.CurveMatchesByStableId.Clear();
        context.Graph.StrokeMatchesByStableId.Clear();
        context.Graph.CurveStatesByStableId.Clear();
        context.Graph.StrokeStatesByStableId.Clear();

        var previous = context.PreviousFrame;
        if (previous is null)
        {
            foreach (var curve in context.Graph.Curves)
            {
                context.Graph.CurveStatesByStableId[curve.StableId] = TemporalFeatureState.New;
            }

            foreach (var candidate in context.Graph.Candidates)
            {
                context.Graph.StrokeStatesByStableId[candidate.StableId] = TemporalStrokeState.FadingIn;
            }

            return;
        }

        foreach (var curve in context.Graph.Curves)
        {
            if (!previous.CurvesByStableId.TryGetValue(curve.StableId, out var prior))
            {
                prior = FindPreviousCurve(curve, previous);
                if (prior is null)
                {
                    continue;
                }

                context.Graph.CurveMatchesByStableId[curve.StableId] = new TemporalCurveMatch(
                    curve.StableId,
                    prior.StableId,
                    TemporalMatchKind.SourceScreenOverlapMatch,
                    ComputeCurveConfidence(curve, prior));
                context.Graph.CurveStatesByStableId[curve.StableId] = TemporalFeatureState.MatchedFallback;

                continue;
            }

            context.Graph.CurveMatchesByStableId[curve.StableId] = new TemporalCurveMatch(
                curve.StableId,
                prior.StableId,
                TemporalMatchKind.DirectStableIdMatch,
                1f);
            context.Graph.CurveStatesByStableId[curve.StableId] = TemporalFeatureState.MatchedDirect;
        }

        foreach (var candidate in context.Graph.Candidates)
        {
            if (!previous.StrokesByStableId.TryGetValue(candidate.StableId, out var prior))
            {
                prior = FindPreviousStroke(candidate, context, previous);
                if (prior is null)
                {
                    continue;
                }

                context.Graph.StrokeMatchesByStableId[candidate.StableId] = new TemporalStrokeMatch(
                    candidate.StableId,
                    prior.StableId,
                    prior.SourceFeatureId,
                    TemporalMatchKind.SourceScreenOverlapMatch,
                    prior.Lifetime,
                    prior.State,
                    ComputeStrokeConfidence(candidate, prior));
                context.Graph.StrokeStatesByStableId[candidate.StableId] = TemporalStrokeState.Replaced;

                continue;
            }

            context.Graph.StrokeMatchesByStableId[candidate.StableId] = new TemporalStrokeMatch(
                candidate.StableId,
                prior.StableId,
                prior.SourceFeatureId,
                TemporalMatchKind.DirectStableIdMatch,
                prior.Lifetime,
                prior.State,
                1f);
            context.Graph.StrokeStatesByStableId[candidate.StableId] = TemporalStrokeState.Alive;
        }

        foreach (var curve in context.Graph.Curves)
        {
            if (!context.Graph.CurveStatesByStableId.ContainsKey(curve.StableId))
            {
                context.Graph.CurveStatesByStableId[curve.StableId] = TemporalFeatureState.New;
            }
        }

        foreach (var candidate in context.Graph.Candidates)
        {
            if (!context.Graph.StrokeStatesByStableId.ContainsKey(candidate.StableId))
            {
                context.Graph.StrokeStatesByStableId[candidate.StableId] = TemporalStrokeState.FadingIn;
            }
        }
    }

    private static PreviousFeatureCurve? FindPreviousCurve(FeatureCurve curve, FrameHistory previous)
    {
        PreviousFeatureCurve? best = null;
        var bestScore = 0f;

        foreach (var candidate in previous.CurvesByStableId.Values)
        {
            if (candidate.Kind != curve.Kind)
            {
                continue;
            }

            var sourceScore = ComputeSourceSimilarity(curve.Source, candidate.Source);
            var screenScore = ComputeCurveScreenOverlap(curve, candidate);
            var score = sourceScore * 0.6f + screenScore * 0.4f;
            if (score <= bestScore || score < 0.55f)
            {
                continue;
            }

            bestScore = score;
            best = candidate;
        }

        return best;
    }

    private static PreviousStroke? FindPreviousStroke(StrokeCandidate candidate, NprContext context, FrameHistory previous)
    {
        var matchedPreviousFeatureId = candidate.FeatureCurveId;
        if (context.Graph.CurveMatchesByStableId.TryGetValue(candidate.FeatureCurveId, out var featureMatch))
        {
            matchedPreviousFeatureId = featureMatch.PreviousStableId;
        }

        PreviousStroke? best = null;
        var bestScore = 0f;

        foreach (var stroke in previous.StrokesByStableId.Values)
        {
            if (stroke.Intent != candidate.Intent)
            {
                continue;
            }

            var sourceScore = stroke.SourceFeatureId == matchedPreviousFeatureId ? 1f : 0f;
            var overlapScore = ComputeStrokeOverlap(candidate, stroke);
            var score = sourceScore * 0.7f + overlapScore * 0.3f;
            if (score <= bestScore || score < 0.6f)
            {
                continue;
            }

            bestScore = score;
            best = stroke;
        }

        return best;
    }

    private static float ComputeCurveConfidence(FeatureCurve current, PreviousFeatureCurve previous)
    {
        return Math.Clamp(
            ComputeSourceSimilarity(current.Source, previous.Source) * 0.6f +
            ComputeCurveScreenOverlap(current, previous) * 0.4f,
            0f,
            1f);
    }

    private static float ComputeStrokeConfidence(StrokeCandidate current, PreviousStroke previous)
    {
        var sourceScore = current.FeatureCurveId == previous.SourceFeatureId ? 1f : 0.75f;
        return Math.Clamp(sourceScore * 0.7f + ComputeStrokeOverlap(current, previous) * 0.3f, 0f, 1f);
    }

    private static float ComputeSourceSimilarity(FeatureCurveSource current, FeatureCurveSource previous)
    {
        if (current == FeatureCurveSource.None || previous == FeatureCurveSource.None)
        {
            return 0.4f;
        }

        var score = 0f;
        if (current.StartVertexIndex == previous.StartVertexIndex || current.StartVertexIndex == previous.EndVertexIndex)
        {
            score += 0.25f;
        }

        if (current.EndVertexIndex == previous.EndVertexIndex || current.EndVertexIndex == previous.StartVertexIndex)
        {
            score += 0.25f;
        }

        if (current.FirstTriangleIndex == previous.FirstTriangleIndex || current.FirstTriangleIndex == previous.SecondTriangleIndex)
        {
            score += 0.25f;
        }

        if (current.SecondTriangleIndex == previous.SecondTriangleIndex || current.SecondTriangleIndex == previous.FirstTriangleIndex)
        {
            score += 0.25f;
        }

        return score;
    }

    private static float ComputeCurveScreenOverlap(FeatureCurve current, PreviousFeatureCurve previous)
    {
        if (current.Points.Count == 0 || previous.Points.Count == 0)
        {
            return 0f;
        }

        var currentMid = Midpoint(current.Points[0].ScreenPosition, current.Points[^1].ScreenPosition);
        var previousMid = Midpoint(previous.Points[0].ScreenPosition, previous.Points[^1].ScreenPosition);
        return DistanceScore(currentMid, previousMid, 28f);
    }

    private static float ComputeStrokeOverlap(StrokeCandidate current, PreviousStroke previous)
    {
        if (current.Points.Count == 0 || previous.Path.Points.Count == 0)
        {
            return 0f;
        }

        var currentMid = Midpoint(current.Points[0], current.Points[^1]);
        var previousMid = Midpoint(previous.Path.Points[0], previous.Path.Points[^1]);
        return DistanceScore(currentMid, previousMid, 24f);
    }

    private static Point2D Midpoint(Point2D a, Point2D b)
    {
        return new Point2D((a.X + b.X) * 0.5f, (a.Y + b.Y) * 0.5f);
    }

    private static float DistanceScore(Point2D a, Point2D b, float radius)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        var distance = MathF.Sqrt(dx * dx + dy * dy);
        return Math.Clamp(1f - distance / Math.Max(0.001f, radius), 0f, 1f);
    }
}
