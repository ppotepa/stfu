using STFU.NPR.Pipeline;
using STFU.NPR.Graph;
using STFU.NPR.Temporal;
using STFU.NPR.Styles;
using STFU.Strokes;

namespace STFU.NPR.Steps.Strokes;

public sealed class BuildStrokeFrameStep : INprStep
{
    public void Execute(NprContext context)
    {
        var view = context.View;
        var paths = new List<StrokePath2D>(context.Graph.StyledStrokes.Count);

        foreach (var stroke in context.Graph.StyledStrokes
            .OrderByDescending(stroke => context.Style.GetLayerOrder(stroke.Kind, stroke.Intent, stroke.Visibility, stroke.HatchLayerKind))
            .ThenBy(stroke => stroke.StableId))
        {
            var style = new StrokeStyle2D(stroke.Thickness, stroke.Opacity, stroke.Color);
            var layer = context.Style.ResolveOutputLayer(stroke.Kind, stroke.Intent, stroke.Visibility, stroke.HatchLayerKind);
            var profile = context.Style.Stroke.FindProfile(stroke.Kind, stroke.Intent, layer);
            var richPoints = BuildRichPoints(stroke.Points, style, MediumProfile.For(profile?.MediumOverride ?? context.Settings.StrokeStyle.Medium));

            paths.Add(new StrokePath2D(
                stroke.Points.ToArray(),
                style,
                richPoints,
                new StrokeMetadata(
                    stroke.StableId,
                    layer,
                    BuildSourceKind(context, stroke),
                    stroke.Intent.ToString(),
                    stroke.FeatureCurveId,
                    stroke.StableId,
                    stroke.Visibility.ToString(),
                    context.Style.StyleId,
                    stroke.HatchLayerKind?.ToString(),
                    context.Style.GetLayerOrder(stroke.Kind, stroke.Intent, stroke.Visibility, stroke.HatchLayerKind))));
        }

        AddFadingOutResiduals(context, paths);

        context.Frame = new StrokeFrame(view.Width, view.Height, paths);
    }

    private static void AddFadingOutResiduals(NprContext context, List<StrokePath2D> paths)
    {
        var previous = context.PreviousFrame;
        if (previous is null)
        {
            return;
        }

        var matchedPreviousIds = context.Graph.StrokeMatchesByStableId.Values
            .Select(match => match.PreviousStableId)
            .ToHashSet();

        foreach (var previousStroke in previous.StrokesByStableId.Values)
        {
            if (matchedPreviousIds.Contains(previousStroke.StableId))
            {
                continue;
            }

            var style = previousStroke.Path.Style with
            {
                Thickness = MathF.Max(0.3f, previousStroke.Path.Style.Thickness * 0.92f),
                Opacity = Math.Clamp(previousStroke.Path.Style.Opacity * 0.42f, context.Style.Tone.MinimumOpacity, context.Style.Tone.MaximumOpacity * 0.55f)
            };
            var richPoints = BuildRichPoints(previousStroke.Path.Points, style, MediumProfile.For(context.Settings.StrokeStyle.Medium));

            paths.Add(new StrokePath2D(
                previousStroke.Path.Points.ToArray(),
                style,
                richPoints,
                new StrokeMetadata(
                    previousStroke.StableId,
                    $"ghost-{context.Style.GetLayerName(previousStroke.Intent)}",
                    "GhostStroke",
                    previousStroke.Intent.ToString(),
                    previousStroke.SourceFeatureId,
                    previousStroke.StableId,
                    TemporalStrokeState.FadingOut.ToString(),
                    context.Style.StyleId,
                    previousStroke.Path.Metadata?.Variant,
                    previousStroke.Path.Metadata?.LayerOrder ?? 100)));
        }
    }

    private static StrokePoint2D[] BuildRichPoints(IReadOnlyList<Point2D> points, StrokeStyle2D style, MediumProfile medium)
    {
        if (points.Count == 0)
        {
            return [];
        }

        if (points.Count == 1)
        {
            return [StrokePoint2D.FromPoint(points[0], style, medium.Pressure.Sample(0f))];
        }

        var richPoints = new StrokePoint2D[points.Count];
        for (var index = 0; index < points.Count; index++)
        {
            var t = index / (float)(points.Count - 1);
            var pressure = medium.Pressure.Sample(t);
            var pointStyle = new StrokeStyle2D(
                MathF.Max(0.2f, style.Thickness * pressure),
                Math.Clamp(style.Opacity * (0.8f + pressure * 0.2f), medium.OpacityFloor, 1f),
                style.Color);
            richPoints[index] = StrokePoint2D.FromPoint(points[index], pointStyle, pressure);
        }

        return richPoints;
    }

    private static string BuildSourceKind(NprContext context, StyledStroke stroke)
    {
        if (stroke.Visibility == VisibilityState.Hidden)
        {
            return context.Style.GetHiddenLinePolicy(stroke.Kind, stroke.Intent) switch
            {
                Composition.HiddenLinePolicy.Dashed => "DashedHiddenStroke",
                Composition.HiddenLinePolicy.Ghost => "GhostHiddenStroke",
                _ => stroke.Kind.ToString()
            };
        }

        return stroke.Kind.ToString();
    }
}
