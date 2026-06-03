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
            if (shade < context.Settings.SurfaceFlowShadeThreshold)
            {
                continue;
            }

            var densityRoll = NprRandom.Float01(NprRandom.Hash(context.Settings.Seed, edge.StableId));
            if (densityRoll > context.Settings.SurfaceFlowDensity)
            {
                continue;
            }

            context.Graph.FeatureLines.Add(new FeatureLine(
                NprRandom.Hash(edge.StableId, 41),
                NprStrokeIntent.SurfaceFlow,
                first.Position,
                second.Position,
                (first.Depth + second.Depth) * 0.5f,
                shade,
                0.25f + shade * 0.35f));
        }
    }
}
