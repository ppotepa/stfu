using STFU.NPR.Graph;
using STFU.NPR.Pipeline;
using STFU.NPR.Temporal;
using STFU.NPR.Styles;
using STFU.Strokes;

namespace STFU.NPR.Steps.Strokes;

public sealed class HumanizeStrokesStep : INprStep
{
    private static readonly STFU.NPR.Styles.IStrokeHumanizer Humanizer = new STFU.NPR.Styles.DefaultStrokeHumanizer();

    public void Execute(NprContext context)
    {
        foreach (var stroke in context.Graph.StyledStrokes)
        {
            var profile = context.Style.Stroke.FindProfile(
                stroke.Kind,
                stroke.Intent,
                context.Style.ResolveOutputLayer(stroke.Kind, stroke.Intent, stroke.Visibility, stroke.HatchLayerKind));
            Humanizer.Humanize(stroke, CreateProfiledStyle(context.Settings.StrokeStyle, profile), context.Settings.Seed);
            BlendWithPreviousStroke(context, stroke);
        }
    }

    private static NprStrokeStyle CreateProfiledStyle(NprStrokeStyle source, Composition.StyleStrokeProfile? profile)
    {
        if (profile is null)
        {
            return source;
        }

        return new NprStrokeStyle
        {
            Seed = source.Seed,
            Medium = profile.MediumOverride ?? source.Medium,
            BaseThickness = source.BaseThickness,
            ThicknessVariation = source.ThicknessVariation * profile.HumanizationScale * profile.ThicknessVariationScale,
            EndpointJitter = source.EndpointJitter * profile.HumanizationScale * profile.EndpointJitterScale,
            Overshoot = source.Overshoot * profile.HumanizationScale * profile.OvershootScale
        };
    }

    private static void BlendWithPreviousStroke(NprContext context, StyledStroke stroke)
    {
        if (context.PreviousFrame is null ||
            !context.Graph.StrokeMatchesByStableId.TryGetValue(stroke.StableId, out var match) ||
            !context.PreviousFrame.StrokesByStableId.TryGetValue(match.PreviousStableId, out var previousStroke) ||
            previousStroke.Path.Points.Count == 0 ||
            stroke.Points.Count == 0)
        {
            return;
        }

        var blend = match.Kind switch
        {
            TemporalMatchKind.DirectStableIdMatch => 0.3f,
            TemporalMatchKind.SourceScreenOverlapMatch => 0.18f,
            _ => 0f
        };

        if (blend <= 0f)
        {
            return;
        }

        if (previousStroke.Path.Points.Count == stroke.Points.Count)
        {
            for (var index = 0; index < stroke.Points.Count; index++)
            {
                stroke.Points[index] = Lerp(previousStroke.Path.Points[index], stroke.Points[index], 1f - blend);
            }

            return;
        }

        var previousStart = previousStroke.Path.Points[0];
        var previousEnd = previousStroke.Path.Points[^1];
        stroke.Points[0] = Lerp(previousStart, stroke.Points[0], 1f - blend);
        stroke.Points[^1] = Lerp(previousEnd, stroke.Points[^1], 1f - blend);

        if (stroke.Points.Count > 2)
        {
            var previousMid = Midpoint(previousStart, previousEnd);
            var currentMidIndex = stroke.Points.Count / 2;
            stroke.Points[currentMidIndex] = Lerp(previousMid, stroke.Points[currentMidIndex], 1f - blend);
        }
    }

    private static Point2D Lerp(Point2D start, Point2D end, float t)
    {
        return new Point2D(
            start.X + (end.X - start.X) * t,
            start.Y + (end.Y - start.Y) * t);
    }

    private static Point2D Midpoint(Point2D start, Point2D end)
    {
        return new Point2D((start.X + end.X) * 0.5f, (start.Y + end.Y) * 0.5f);
    }
}
