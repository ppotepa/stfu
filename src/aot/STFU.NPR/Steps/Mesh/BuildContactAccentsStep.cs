using System.Numerics;
using STFU.NPR.Graph;
using STFU.NPR.Pipeline;
using STFU.NPR.Styles;
using STFU.Strokes;

namespace STFU.NPR.Steps.Mesh;

public sealed class BuildContactAccentsStep : INprStep
{
    public void Execute(NprContext context)
    {
        foreach (var sample in context.Graph.SurfaceSamples)
        {
            if (sample.Shade < 0.82f)
            {
                continue;
            }

            var depthFactor = Math.Clamp(sample.Depth, 0f, 1f);
            var curvatureFactor = Math.Clamp(sample.SmoothedCurvature, 0f, 1f);
            var toneFactor = Math.Clamp(sample.Shade, 0f, 1f);
            var contactStrength = toneFactor * 0.65f + depthFactor * 0.2f + curvatureFactor * 0.15f;
            if (contactStrength < 0.72f)
            {
                continue;
            }

            var roll = NprRandom.Float01(NprRandom.Hash(context.Settings.Seed, sample.StableId ^ 911));
            if (roll > Math.Clamp(contactStrength, 0f, 1f))
            {
                continue;
            }

            var direction = ResolveDirection(context, sample);
            var half = 4.5f + contactStrength * 4f;
            var start = new Point2D(sample.Position.X - direction.X * half, sample.Position.Y - direction.Y * half * 0.35f);
            var end = new Point2D(sample.Position.X + direction.X * half, sample.Position.Y + direction.Y * half * 0.35f);

            context.Graph.AddCurve(FeatureCurve.FromLine(
                NprRandom.Hash(sample.StableId, 907),
                FeatureCurveKind.ContactAccent,
                NprStrokeIntent.Accent,
                new FeaturePoint(start, sample.Depth),
                new FeaturePoint(end, sample.Depth),
                FeatureCurveSource.None,
                sample.Shade,
                Math.Clamp(0.22f + contactStrength * 0.42f, 0f, 1f),
                confidence: Math.Clamp(0.56f + contactStrength * 0.3f, 0f, 1f),
                flags: FeatureCurveFlags.Generated));
        }
    }

    private static Vector2 ResolveDirection(NprContext context, SurfaceSample sample)
    {
        var axis = new Vector2(sample.CurvatureDirection.X, -sample.CurvatureDirection.Y);
        if (axis.LengthSquared() > 0.0001f)
        {
            axis = Vector2.Normalize(axis);
            return new Vector2(-axis.Y, axis.X);
        }

        var field = context.Graph.DirectionField?.Samples;
        if (field is not null && field.Count > 0)
        {
            var nearest = field
                .OrderBy(direction =>
                {
                    var dx = direction.Position.X - sample.Position.X;
                    var dy = direction.Position.Y - sample.Position.Y;
                    return dx * dx + dy * dy;
                })
                .First();

            if (nearest.Direction.LengthSquared() > 0.0001f)
            {
                var normalized = Vector2.Normalize(nearest.Direction);
                return new Vector2(-normalized.Y, normalized.X);
            }
        }

        return new Vector2(1f, 0f);
    }
}
