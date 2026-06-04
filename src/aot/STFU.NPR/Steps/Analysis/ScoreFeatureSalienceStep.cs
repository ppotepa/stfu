using STFU.NPR.Graph;
using STFU.NPR.Pipeline;
using STFU.Strokes;

namespace STFU.NPR.Steps.Analysis;

public sealed class ScoreFeatureSalienceStep : INprStep
{
    public void Execute(NprContext context)
    {
        context.Graph.SalienceByStableId.Clear();

        if (context.Graph.VisibilitySegments.Count > 0)
        {
            foreach (var segment in context.Graph.VisibilitySegments)
            {
                context.Graph.SalienceByStableId[segment.StableId] = Compute(context, segment);
            }

            return;
        }

        foreach (var line in context.Graph.FeatureLines)
        {
            context.Graph.SalienceByStableId[line.StableId] = Compute(context, line);
        }
    }

    private static SalienceScore Compute(NprContext context, VisibilitySegment segment)
    {
        return Compute(
            context,
            segment.Kind,
            segment.Intent,
            segment.Start,
            segment.End,
            segment.Depth,
            segment.Shade,
            segment.Importance,
            segment.Confidence,
            segment.State == VisibilityState.Visible);
    }

    private static SalienceScore Compute(NprContext context, FeatureLine line)
    {
        return Compute(
            context,
            InferKind(line.Intent),
            line.Intent,
            line.Start,
            line.End,
            line.Depth,
            line.Shade,
            line.Importance,
            1f,
            isVisible: true);
    }

    private static SalienceScore Compute(
        NprContext context,
        FeatureCurveKind kind,
        NprStrokeIntent intent,
        Point2D start,
        Point2D end,
        float depth,
        float shade,
        float importance,
        float confidence,
        bool isVisible)
    {
        var length = MeasureLength(start, end);
        var normalizedLength = Math.Clamp(length / 140f, 0f, 1f);
        var geometry = Math.Clamp((importance * 0.65f + normalizedLength * 0.35f) * (0.72f + confidence * 0.28f), 0f, 1f);
        var visibility = isVisible ? 1f : 0.18f;
        var tone = intent switch
        {
            NprStrokeIntent.Hatch or NprStrokeIntent.SurfaceFlow => Math.Clamp(shade, 0f, 1f),
            _ => 1f
        };

        var style = intent switch
        {
            _ => context.Style.GetBaseWeight(kind, intent, intent switch
            {
                NprStrokeIntent.Silhouette => 1f,
                NprStrokeIntent.Boundary => 0.96f,
                NprStrokeIntent.Crease => 0.78f,
                NprStrokeIntent.Hatch => 0.58f,
                NprStrokeIntent.SurfaceFlow => 0.48f,
                _ => 0.62f
            })
        };

        var midpoint = Midpoint(start, end);
        var focus = ComputeFocus(context, midpoint, depth);
        var localDensity = SampleDensity(context, midpoint);
        var clutterPenalty = intent switch
        {
            NprStrokeIntent.Hatch => Math.Clamp(localDensity * 0.45f, 0f, 0.55f),
            NprStrokeIntent.SurfaceFlow => Math.Clamp(localDensity * 0.35f, 0f, 0.45f),
            _ => Math.Clamp(localDensity * 0.1f, 0f, 0.15f)
        };

        var final = geometry * 0.3f +
            visibility * 0.2f +
            tone * 0.1f +
            style * 0.2f +
            focus * 0.2f -
            clutterPenalty * 0.2f;
        final *= 0.76f + confidence * 0.24f;

        return SalienceScore.Clamp(new SalienceScore(
            geometry,
            visibility,
            tone,
            confidence,
            style,
            focus,
            clutterPenalty,
            final));
    }

    private static float SampleDensity(NprContext context, Point2D point)
    {
        var samples = context.Graph.DensityField?.Samples;
        if (samples is null || samples.Count == 0)
        {
            return 0f;
        }

        var bestDistance = float.MaxValue;
        var bestDensity = 0f;
        foreach (var sample in samples)
        {
            var dx = sample.Position.X - point.X;
            var dy = sample.Position.Y - point.Y;
            var distance = dx * dx + dy * dy;
            if (distance >= bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            bestDensity = sample.Density;
        }

        return bestDensity;
    }

    private static Point2D Midpoint(Point2D start, Point2D end)
    {
        return new Point2D((start.X + end.X) * 0.5f, (start.Y + end.Y) * 0.5f);
    }

    private static float ComputeFocus(NprContext context, Point2D point, float depth)
    {
        var baseFocus = 1f / (1f + MathF.Max(0f, depth) * 0.14f);
        var maskBoost = 0f;

        foreach (var mask in context.Graph.StyleMasks)
        {
            if (mask.Role != StyleMaskRole.Focus)
            {
                continue;
            }

            if (mask.ScreenRegions.Any(region => ContainsPoint(region, point)))
            {
                maskBoost = Math.Max(maskBoost, mask.Strength * 0.45f);
            }
        }

        return Math.Clamp(baseFocus + maskBoost, 0f, 1f);
    }

    private static bool ContainsPoint(ScreenPolygon polygon, Point2D point)
    {
        var inside = false;
        var points = polygon.Points;

        for (int i = 0, j = points.Count - 1; i < points.Count; j = i++)
        {
            var pi = points[i];
            var pj = points[j];
            var intersects = ((pi.Y > point.Y) != (pj.Y > point.Y)) &&
                (point.X < (pj.X - pi.X) * (point.Y - pi.Y) / ((pj.Y - pi.Y) + 0.0001f) + pi.X);

            if (intersects)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private static float MeasureLength(Point2D start, Point2D end)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
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
