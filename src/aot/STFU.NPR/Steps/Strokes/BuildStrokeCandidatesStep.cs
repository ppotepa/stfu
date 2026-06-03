using STFU.NPR.Graph;
using STFU.NPR.Pipeline;

namespace STFU.NPR.Steps.Strokes;

public sealed class BuildStrokeCandidatesStep : INprStep
{
    public void Execute(NprContext context)
    {
        foreach (var feature in context.Graph.FeatureLines)
        {
            var length = MeasureLength(feature.Start, feature.End);

            if (length < context.Settings.MinimumStrokeLength &&
                feature.Intent is not (NprStrokeIntent.Silhouette or NprStrokeIntent.Boundary))
            {
                continue;
            }

            context.Graph.Strokes.Add(new NprStroke(
                feature.StableId,
                feature.Intent,
                [feature.Start, feature.End],
                feature.Depth,
                feature.Shade,
                feature.Importance));
        }
    }

    private static float MeasureLength(STFU.Strokes.Point2D start, STFU.Strokes.Point2D end)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }
}
