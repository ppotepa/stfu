using STFU.NPR.Graph;
using STFU.NPR.Pipeline;
using STFU.NPR.Styles;
using STFU.Strokes;

namespace STFU.NPR.Steps.Strokes;

public sealed class HumanizeStrokesStep : INprStep
{
    public void Execute(NprContext context)
    {
        foreach (var stroke in context.Graph.Strokes)
        {
            Humanize(stroke, context.Settings.StrokeStyle, context.Settings.Seed);
        }
    }

    private static void Humanize(NprStroke stroke, NprStrokeStyle style, int globalSeed)
    {
        if (stroke.Points.Count < 2)
        {
            return;
        }

        var originalStart = stroke.Points[0];
        var originalEnd = stroke.Points[^1];
        var dx = originalEnd.X - originalStart.X;
        var dy = originalEnd.Y - originalStart.Y;
        var length = MathF.Sqrt(dx * dx + dy * dy);

        if (length <= 0.001f)
        {
            return;
        }

        var dirX = dx / length;
        var dirY = dy / length;
        var normalX = -dirY;
        var normalY = dirX;
        var seed = NprRandom.Hash(globalSeed, stroke.StableId);
        var overshootStart = style.Overshoot * (0.55f + NprRandom.Float01(NprRandom.Hash(seed, 1)) * 0.65f);
        var overshootEnd = style.Overshoot * (0.55f + NprRandom.Float01(NprRandom.Hash(seed, 2)) * 0.65f);
        var startNormalJitter = NprRandom.SignedFloat(seed, 3) * style.EndpointJitter;
        var endNormalJitter = NprRandom.SignedFloat(seed, 4) * style.EndpointJitter;
        var startTangentialJitter = NprRandom.SignedFloat(seed, 5) * style.EndpointJitter * 0.35f;
        var endTangentialJitter = NprRandom.SignedFloat(seed, 6) * style.EndpointJitter * 0.35f;
        var midpointBend = NprRandom.SignedFloat(seed, 8) * style.EndpointJitter * 1.25f;

        var start = new Point2D(
            originalStart.X - dirX * overshootStart + normalX * startNormalJitter + dirX * startTangentialJitter,
            originalStart.Y - dirY * overshootStart + normalY * startNormalJitter + dirY * startTangentialJitter);

        var end = new Point2D(
            originalEnd.X + dirX * overshootEnd + normalX * endNormalJitter + dirX * endTangentialJitter,
            originalEnd.Y + dirY * overshootEnd + normalY * endNormalJitter + dirY * endTangentialJitter);

        var mid = new Point2D(
            (start.X + end.X) * 0.5f + normalX * midpointBend,
            (start.Y + end.Y) * 0.5f + normalY * midpointBend);

        stroke.Points.Clear();
        stroke.Points.Add(start);
        stroke.Points.Add(mid);
        stroke.Points.Add(end);

        stroke.Thickness = MathF.Max(0.35f, stroke.Thickness + NprRandom.SignedFloat(seed, 7) * style.ThicknessVariation);
    }
}
