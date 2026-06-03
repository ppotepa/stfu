using STFU.NPR.Graph;
using STFU.NPR.Pipeline;
using STFU.NPR.Styles;

namespace STFU.NPR.Steps.Analysis;

public sealed class PruneFeatureLinesStep : INprStep
{
    public void Execute(NprContext context)
    {
        var kept = new List<FeatureLine>(context.Graph.FeatureLines.Count);

        foreach (var line in context.Graph.FeatureLines)
        {
            if (ShouldKeep(context, line))
            {
                kept.Add(line);
            }
        }

        context.Graph.FeatureLines.Clear();
        context.Graph.FeatureLines.AddRange(kept);
    }

    private static bool ShouldKeep(NprContext context, FeatureLine line)
    {
        if (line.Intent is NprStrokeIntent.Silhouette or NprStrokeIntent.Boundary)
        {
            return true;
        }

        var density = line.Intent switch
        {
            NprStrokeIntent.Crease => MathF.Min(1f, context.Settings.FeatureLineDensity + line.Importance * 0.25f),
            NprStrokeIntent.Hatch => context.Settings.HatchDensity * (0.55f + line.Shade * 0.6f),
            NprStrokeIntent.SurfaceFlow => context.Settings.SurfaceFlowDensity * (0.45f + line.Shade * 0.55f),
            _ => context.Settings.FeatureLineDensity
        };

        return NprRandom.Float01(NprRandom.Hash(context.Settings.Seed, line.StableId)) <= density;
    }
}
