using STFU.NPR.Graph;
using STFU.NPR.Pipeline;
using System.Numerics;

namespace STFU.NPR.Steps.Strokes;

public sealed class BuildStrokeCandidatesStep : INprStep
{
    public void Execute(NprContext context)
    {
        context.Graph.Candidates.Clear();

        if (context.Graph.VisibilitySegments.Count > 0)
        {
            foreach (var segment in context.Graph.VisibilitySegments)
            {
                var hiddenPolicy = context.Style.GetHiddenLinePolicy(segment.Kind, segment.Intent);
                if (segment.State != VisibilityState.Visible &&
                    hiddenPolicy is Composition.HiddenLinePolicy.Suppress or Composition.HiddenLinePolicy.KeepForDebug)
                {
                    continue;
                }

                var length = MeasureLength(segment.Start, segment.End);
                if (!ShouldKeepScreenSegment(context, segment.Start, segment.End, length))
                {
                    continue;
                }

                if (length < context.Settings.MinimumStrokeLength &&
                    segment.Intent is not (NprStrokeIntent.Silhouette or NprStrokeIntent.Boundary))
                {
                    continue;
                }

                context.Graph.Candidates.Add(new StrokeCandidate(
                    segment.StableId,
                    segment.FeatureCurveId,
                    segment.Kind,
                    segment.Intent,
                    [segment.Start, segment.End],
                    segment.Depth,
                    segment.Shade,
                    segment.Importance,
                    segment.Confidence,
                    context.Graph.GetSalience(segment.StableId, segment.Importance),
                    segment.State,
                    SampleTone(context, segment.Start, segment.Shade),
                    SampleDirection(context, segment.Start),
                    SampleDensity(context, segment.Start),
                    segment.HatchLayerKind));
            }

            return;
        }

        foreach (var feature in context.Graph.FeatureLines)
        {
            var length = MeasureLength(feature.Start, feature.End);
            if (!ShouldKeepScreenSegment(context, feature.Start, feature.End, length))
            {
                continue;
            }

            if (length < context.Settings.MinimumStrokeLength &&
                feature.Intent is not (NprStrokeIntent.Silhouette or NprStrokeIntent.Boundary))
            {
                continue;
            }

            context.Graph.Candidates.Add(new StrokeCandidate(
                feature.StableId,
                feature.StableId,
                InferKind(feature.Intent),
                feature.Intent,
                [feature.Start, feature.End],
                feature.Depth,
                feature.Shade,
                feature.Importance,
                1f,
                context.Graph.GetSalience(feature.StableId, feature.Importance),
                VisibilityState.Visible,
                SampleTone(context, feature.Start, feature.Shade),
                SampleDirection(context, feature.Start),
                SampleDensity(context, feature.Start)));
        }
    }

    private static float SampleTone(NprContext context, STFU.Strokes.Point2D point, float fallback)
    {
        var samples = context.Graph.ToneField?.Samples;
        if (samples is null || samples.Count == 0)
        {
            return fallback;
        }

        return FindNearest(samples, point, sample => sample.Position, sample => sample.Tone, fallback);
    }

    private static Vector2 SampleDirection(NprContext context, STFU.Strokes.Point2D point)
    {
        var samples = context.Graph.DirectionField?.Samples;
        if (samples is null || samples.Count == 0)
        {
            return new Vector2(1f, 0f);
        }

        return FindNearest(samples, point, sample => sample.Position, sample => sample.Direction, new Vector2(1f, 0f));
    }

    private static float SampleDensity(NprContext context, STFU.Strokes.Point2D point)
    {
        var samples = context.Graph.DensityField?.Samples;
        if (samples is null || samples.Count == 0)
        {
            return 0f;
        }

        return FindNearest(samples, point, sample => sample.Position, sample => sample.Density, 0f);
    }

    private static TValue FindNearest<TSample, TValue>(
        IReadOnlyList<TSample> samples,
        STFU.Strokes.Point2D point,
        Func<TSample, STFU.Strokes.Point2D> positionSelector,
        Func<TSample, TValue> valueSelector,
        TValue fallback)
    {
        var bestDistance = float.MaxValue;
        var bestValue = fallback;

        foreach (var sample in samples)
        {
            var position = positionSelector(sample);
            var dx = position.X - point.X;
            var dy = position.Y - point.Y;
            var distance = dx * dx + dy * dy;
            if (distance >= bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            bestValue = valueSelector(sample);
        }

        return bestValue;
    }

    private static float MeasureLength(STFU.Strokes.Point2D start, STFU.Strokes.Point2D end)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private static bool ShouldKeepScreenSegment(NprContext context, STFU.Strokes.Point2D start, STFU.Strokes.Point2D end, float length)
    {
        if (!float.IsFinite(start.X) ||
            !float.IsFinite(start.Y) ||
            !float.IsFinite(end.X) ||
            !float.IsFinite(end.Y))
        {
            return false;
        }

        var margin = context.Settings.ScreenClipMarginPixels;
        var minX = MathF.Min(start.X, end.X);
        var minY = MathF.Min(start.Y, end.Y);
        var maxX = MathF.Max(start.X, end.X);
        var maxY = MathF.Max(start.Y, end.Y);
        var intersectsViewport = maxX >= -margin &&
            maxY >= -margin &&
            minX <= context.Width + margin &&
            minY <= context.Height + margin;
        if (!intersectsViewport)
        {
            return false;
        }

        var diagonal = MathF.Sqrt(context.Width * context.Width + context.Height * context.Height);
        return length <= MathF.Max(1f, diagonal * 4f);
    }

    private static FeatureCurveKind InferKind(NprStrokeIntent intent)
    {
        return intent switch
        {
            NprStrokeIntent.Boundary => FeatureCurveKind.Boundary,
            NprStrokeIntent.Silhouette => FeatureCurveKind.Silhouette,
            NprStrokeIntent.Crease => FeatureCurveKind.Crease,
            NprStrokeIntent.SurfaceFlow => FeatureCurveKind.SurfaceFlow,
            NprStrokeIntent.Hatch => FeatureCurveKind.Hatch,
            _ => FeatureCurveKind.Accent
        };
    }
}
