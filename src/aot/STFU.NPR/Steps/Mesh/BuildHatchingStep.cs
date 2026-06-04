using STFU.NPR.Graph;
using STFU.NPR.Fields;
using STFU.NPR.Pipeline;
using STFU.NPR.Styles;
using STFU.Strokes;
using System.Numerics;

namespace STFU.NPR.Steps.Mesh;

public sealed class BuildHatchingStep : INprStep
{
    public void Execute(NprContext context)
    {
        if (!context.Style.Hatching.Enabled)
        {
            return;
        }

        context.Graph.HatchingPlans.Clear();

        foreach (var sample in context.Graph.SurfaceSamples)
        {
            if (sample.Shade < context.Style.Hatching.ToneThreshold)
            {
                continue;
            }

            var region = FindRegion(context.Graph.MaterialRegions, sample.ProjectedTriangleIndex);

            var densityRoll = NprRandom.Float01(NprRandom.Hash(context.Settings.Seed, sample.StableId));
            var sampleDensity = SampleDensity(context.Graph.DensityField, sample.Position, Math.Clamp(sample.Shade * context.Settings.HatchDensity, 0f, 1f));
            var densityTarget = Math.Clamp(sampleDensity * context.Style.Hatching.DensityScale, 0f, 1f);
            densityTarget = ApplyRegionDensityPolicy(region, densityTarget);
            densityTarget = ApplyTextureDensity(context.Graph.TextureField, sample.Position, densityTarget);
            if (densityRoll > densityTarget)
            {
                continue;
            }

            var tone = SampleTone(context.Graph.ToneField, sample.Position, sample.Shade);
            var length = context.Style.Hatching.StrokeLengthPixels * (0.65f + tone * 0.55f);
            var texture = SampleTexture(context.Graph.TextureField, sample.Position, 0.4f);
            var angleJitter = NprRandom.SignedFloat(sample.StableId, 17) * context.Style.Hatching.JitterRadians * (0.8f + texture * 0.5f);
            var direction = ResolveDirection(context.Graph.DirectionField, sample.Position, context.Style.Hatching.UseDirectionField);
            var baseAngle = MathF.Atan2(direction.Y, direction.X) + context.Style.Hatching.DirectionAngleOffsetRadians;
            var spacing = Math.Max(1f, context.Style.Hatching.BaseSpacingPixels / Math.Max(0.25f, densityTarget));
            var primary = new HatchLayer(
                context.Style.Hatching.ToneThreshold,
                spacing,
                length,
                context.Style.Hatching.DirectionAngleOffsetRadians,
                0.3f + tone * 0.4f,
                0.65f,
                HatchLayerKind.Primary);

            HatchLayer? secondary = null;
            if (tone >= context.Style.Hatching.CrossHatchThreshold)
            {
                secondary = new HatchLayer(
                    context.Style.Hatching.CrossHatchThreshold,
                    Math.Max(1f, spacing * 0.92f),
                    length * 0.95f,
                    context.Style.Hatching.CrossAngleOffsetRadians,
                    0.26f + tone * 0.32f,
                    0.58f,
                    HatchLayerKind.Cross);
            }

            HatchLayer? tertiary = null;
            if (tone >= context.Style.Hatching.DeepShadowThreshold)
            {
                tertiary = new HatchLayer(
                    context.Style.Hatching.DeepShadowThreshold,
                    Math.Max(1f, spacing * 0.84f),
                    length * 0.9f,
                    context.Style.Hatching.TertiaryAngleOffsetRadians,
                    0.24f + tone * 0.28f,
                    0.52f,
                    HatchLayerKind.Tertiary);
            }

            context.Graph.HatchingPlans.Add(new HatchingPlan(
                NprRandom.Hash(sample.StableId, 211),
                region?.StableId ?? sample.ProjectedTriangleIndex,
                sample.Position,
                primary,
                secondary,
                tertiary,
                tone,
                densityTarget));

            EmitHatchGuideCurve(context, sample, tone, baseAngle, angleJitter, primary, 53);
            EmitHatchCurve(context, sample, tone, baseAngle, angleJitter, primary, 59);

            if (secondary is not null)
            {
                EmitHatchCurve(context, sample, tone, baseAngle, angleJitter, secondary, 67);
            }

            if (tertiary is not null)
            {
                EmitHatchCurve(context, sample, tone, baseAngle, angleJitter, tertiary, 71);
            }
        }
    }

    private static void EmitHatchCurve(
        NprContext context,
        SurfaceSample sample,
        float tone,
        float baseAngle,
        float angleJitter,
        HatchLayer layer,
        int hashSalt)
    {
        var angle = context.Style.Hatching.UseDirectionField
            ? baseAngle + layer.DirectionAngleOffsetRadians + angleJitter
            : layer.DirectionAngleOffsetRadians + angleJitter;
        var directionX = MathF.Cos(angle);
        var directionY = MathF.Sin(angle);
        var half = layer.StrokeLengthPixels * 0.5f;
        var start = new Point2D(sample.Position.X - directionX * half, sample.Position.Y - directionY * half);
        var end = new Point2D(sample.Position.X + directionX * half, sample.Position.Y + directionY * half);

        context.Graph.AddCurve(FeatureCurve.FromLine(
            NprRandom.Hash(sample.StableId, hashSalt),
            FeatureCurveKind.Hatch,
            NprStrokeIntent.Hatch,
            new FeaturePoint(start, sample.Depth),
            new FeaturePoint(end, sample.Depth),
            FeatureCurveSource.None,
            sample.Shade,
            0.18f + tone * 0.42f,
            confidence: Math.Clamp(0.55f + tone * 0.25f, 0f, 1f),
            flags: FeatureCurveFlags.Generated,
            hatchLayerKind: layer.Kind));
    }

    private static void EmitHatchGuideCurve(
        NprContext context,
        SurfaceSample sample,
        float tone,
        float baseAngle,
        float angleJitter,
        HatchLayer layer,
        int hashSalt)
    {
        var angle = context.Style.Hatching.UseDirectionField
            ? baseAngle + layer.DirectionAngleOffsetRadians + angleJitter * 0.35f
            : layer.DirectionAngleOffsetRadians + angleJitter * 0.35f;
        var directionX = MathF.Cos(angle);
        var directionY = MathF.Sin(angle);
        var half = layer.StrokeLengthPixels * 0.65f;
        var start = new Point2D(sample.Position.X - directionX * half, sample.Position.Y - directionY * half);
        var end = new Point2D(sample.Position.X + directionX * half, sample.Position.Y + directionY * half);

        context.Graph.AddCurve(FeatureCurve.FromLine(
            NprRandom.Hash(sample.StableId, hashSalt),
            FeatureCurveKind.HatchGuide,
            NprStrokeIntent.Hatch,
            new FeaturePoint(start, sample.Depth),
            new FeaturePoint(end, sample.Depth),
            FeatureCurveSource.None,
            sample.Shade,
            0.08f + tone * 0.18f,
            confidence: Math.Clamp(0.48f + tone * 0.22f, 0f, 1f),
            flags: FeatureCurveFlags.Generated,
            hatchLayerKind: layer.Kind));
    }

    private static float SampleTone(ToneField? field, Point2D point, float fallback)
    {
        var samples = field?.Samples;
        if (samples is null || samples.Count == 0)
        {
            return fallback;
        }

        return FindNearest(samples, point, sample => sample.Position, sample => sample.Tone, fallback);
    }

    private static float SampleDensity(DensityField? field, Point2D point, float fallback)
    {
        var samples = field?.Samples;
        if (samples is null || samples.Count == 0)
        {
            return fallback;
        }

        return FindNearest(samples, point, sample => sample.Position, sample => sample.Density, fallback);
    }

    private static float SampleTexture(TextureField? field, Point2D point, float fallback)
    {
        var samples = field?.Samples;
        if (samples is null || samples.Count == 0)
        {
            return fallback;
        }

        return FindNearest(samples, point, sample => sample.Position, sample => sample.Texture, fallback);
    }

    private static Vector2 ResolveDirection(DirectionField? field, Point2D point, bool useDirectionField)
    {
        if (!useDirectionField)
        {
            return new Vector2(1f, 0f);
        }

        var samples = field?.Samples;
        if (samples is null || samples.Count == 0)
        {
            return new Vector2(1f, 0f);
        }

        var direction = FindNearest(samples, point, sample => sample.Position, sample => sample.Direction, new Vector2(1f, 0f));
        return direction.LengthSquared() < 0.0001f ? new Vector2(1f, 0f) : Vector2.Normalize(direction);
    }

    private static TValue FindNearest<TSample, TValue>(
        IReadOnlyList<TSample> samples,
        Point2D point,
        Func<TSample, Point2D> positionSelector,
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

    private static MaterialRegion? FindRegion(IReadOnlyList<MaterialRegion> regions, int projectedTriangleIndex)
    {
        return regions.FirstOrDefault(region => region.TriangleIndices.Contains(projectedTriangleIndex));
    }

    private static float ApplyRegionDensityPolicy(MaterialRegion? region, float densityTarget)
    {
        if (region is null)
        {
            return densityTarget;
        }

        var scale = region.HatchingPolicy switch
        {
            RegionHatchingPolicy.Sparse => 0.72f,
            RegionHatchingPolicy.Default => 1f,
            RegionHatchingPolicy.CrossHatch => 1.18f,
            RegionHatchingPolicy.Dense => 1.3f,
            _ => 1f
        };

        return Math.Clamp(densityTarget * scale, 0f, 1f);
    }

    private static float ApplyTextureDensity(TextureField? field, Point2D point, float densityTarget)
    {
        var texture = SampleTexture(field, point, 0.4f);
        return Math.Clamp(densityTarget * (0.82f + texture * 0.38f), 0f, 1f);
    }
}
