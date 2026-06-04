using STFU.NPR.Graph;
using STFU.NPR.Pipeline;
using STFU.NPR.Styles;

namespace STFU.NPR.Steps.Mesh;

public sealed class BuildSurfaceFlowLinesStep : INprStep
{
    public void Execute(NprContext context)
    {
        var samplesByTriangle = new Dictionary<int, SurfaceSample>();

        foreach (var sample in context.Graph.SurfaceSamples)
        {
            samplesByTriangle[sample.ProjectedTriangleIndex] = sample;
        }

        foreach (var edge in context.Graph.TopologyEdges)
        {
            if (edge.IsBoundary || edge.SecondTriangleIndex < 0 || edge.NormalAngleDegrees >= context.Settings.CreaseAngleDegrees)
            {
                continue;
            }

            if (!samplesByTriangle.TryGetValue(edge.FirstTriangleIndex, out var first) ||
                !samplesByTriangle.TryGetValue(edge.SecondTriangleIndex, out var second))
            {
                continue;
            }

            var shade = (first.Shade + second.Shade) * 0.5f;
            var curvature = (first.SmoothedCurvature + second.SmoothedCurvature) * 0.5f;
            if (shade < context.Settings.SurfaceFlowShadeThreshold)
            {
                continue;
            }

            var densityRoll = NprRandom.Float01(NprRandom.Hash(context.Settings.Seed, edge.StableId));
            var densityThreshold = Math.Clamp(context.Settings.SurfaceFlowDensity + curvature * 0.2f, 0f, 1f);
            if (densityRoll > densityThreshold)
            {
                continue;
            }

            var direction = first.CurvatureDirection + second.CurvatureDirection;
            var hasDirection = direction.LengthSquared() > 0.0001f;
            if (hasDirection)
            {
                direction = System.Numerics.Vector3.Normalize(direction);
            }

            var startPoint = hasDirection
                ? new FeaturePoint(
                    new STFU.Strokes.Point2D(first.Position.X - direction.X * 4f, first.Position.Y + direction.Y * 4f),
                    first.Depth)
                : new FeaturePoint(first.Position, first.Depth);
            var endPoint = hasDirection
                ? new FeaturePoint(
                    new STFU.Strokes.Point2D(second.Position.X + direction.X * 4f, second.Position.Y - direction.Y * 4f),
                    second.Depth)
                : new FeaturePoint(second.Position, second.Depth);

            context.Graph.AddCurve(FeatureCurve.FromLine(
                NprRandom.Hash(edge.StableId, 41),
                FeatureCurveKind.SurfaceFlow,
                NprStrokeIntent.SurfaceFlow,
                startPoint,
                endPoint,
                new FeatureCurveSource(-1, -1, edge.FirstTriangleIndex, edge.SecondTriangleIndex),
                shade,
                Math.Clamp(0.22f + shade * 0.28f + curvature * 0.3f, 0f, 1f),
                confidence: Math.Clamp(0.45f + curvature * 0.4f, 0f, 1f),
                flags: FeatureCurveFlags.Generated));
        }
    }
}
