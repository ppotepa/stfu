using STFU.NPR.Debug;
using STFU.NPR.Graph;

namespace STFU.NPR.Pipeline.Default.Steps;

public sealed class DefaultBuildDebugFrameStep : STFU.NPR.Pipeline.INprStep
{
    public void Execute(STFU.NPR.Pipeline.NprContext context)
    {
        if (!context.IncludeDebugFrame)
        {
            context.DebugFrame = NprDebugFrame.Empty;
            return;
        }

        var lines = new List<NprDebugLine>();

        foreach (var curve in context.Graph.Curves)
        {
            if (curve.Points.Count < 2)
            {
                continue;
            }

            lines.Add(new NprDebugLine(
                DebugOverlayKind.FeatureCurves,
                curve.Points[0].ScreenPosition,
                curve.Points[^1].ScreenPosition,
                curve.Kind.ToString(),
                curve.AverageDepth,
                curve.Kind == FeatureCurveKind.Silhouette,
                curve.StableId));
        }

        foreach (var segment in context.Graph.VisibilitySegments)
        {
            lines.Add(new NprDebugLine(
                DebugOverlayKind.VisibilitySegments,
                segment.Start,
                segment.End,
                segment.Kind.ToString(),
                segment.Depth,
                segment.State == VisibilityState.Visible,
                segment.StableId));
        }

        foreach (var path in context.Graph.DefaultDrawablePaths)
        {
            for (var i = 1; i < path.Points.Count; i++)
            {
                lines.Add(new NprDebugLine(
                    DebugOverlayKind.StrokeCandidates,
                    path.Points[i - 1],
                    path.Points[i],
                    path.Type.ToString(),
                    0f,
                    path.Type == DefaultLineKind.Silhouette,
                    path.StableId));
            }
        }

        var visibleSegmentCount = 0;
        var hiddenSegmentCount = 0;
        for (var i = 0; i < context.Graph.VisibilitySegments.Count; i++)
        {
            if (context.Graph.VisibilitySegments[i].State == VisibilityState.Visible)
            {
                visibleSegmentCount++;
            }
            else if (context.Graph.VisibilitySegments[i].State == VisibilityState.Hidden)
            {
                hiddenSegmentCount++;
            }
        }

        context.DebugFrame = new NprDebugFrame(
            lines,
            new NprDebugCounters(
                context.Graph.Curves.Count,
                visibleSegmentCount,
                hiddenSegmentCount,
                context.Graph.Curves.Count,
                context.Graph.DefaultFragments.Count,
                context.Frame.Paths.Count,
                0,
                0,
                0),
            []);
    }
}
