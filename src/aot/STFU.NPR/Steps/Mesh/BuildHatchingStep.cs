using STFU.NPR.Graph;
using STFU.NPR.Pipeline;
using STFU.NPR.Styles;
using STFU.Strokes;

namespace STFU.NPR.Steps.Mesh;

public sealed class BuildHatchingStep : INprStep
{
    public void Execute(NprContext context)
    {
        foreach (var sample in context.Graph.SurfaceSamples)
        {
            if (sample.Shade < context.Settings.HatchShadeThreshold)
            {
                continue;
            }

            var densityRoll = NprRandom.Float01(NprRandom.Hash(context.Settings.Seed, sample.StableId));
            if (densityRoll > context.Settings.HatchDensity)
            {
                continue;
            }

            var length = context.Settings.HatchLength * (0.65f + sample.Shade * 0.55f);
            var angleJitter = NprRandom.SignedFloat(sample.StableId, 17) * 0.32f;
            var angle = -0.78f + angleJitter;
            var directionX = MathF.Cos(angle);
            var directionY = MathF.Sin(angle);
            var half = length * 0.5f;
            var start = new Point2D(sample.Position.X - directionX * half, sample.Position.Y - directionY * half);
            var end = new Point2D(sample.Position.X + directionX * half, sample.Position.Y + directionY * half);

            context.Graph.FeatureLines.Add(new FeatureLine(
                NprRandom.Hash(sample.StableId, 59),
                NprStrokeIntent.Hatch,
                start,
                end,
                sample.Depth,
                sample.Shade,
                0.18f + sample.Shade * 0.42f));
        }
    }
}
