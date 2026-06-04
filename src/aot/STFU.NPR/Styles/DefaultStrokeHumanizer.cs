using STFU.NPR.Graph;
using STFU.Strokes;

namespace STFU.NPR.Styles;

public sealed class DefaultStrokeHumanizer : IStrokeHumanizer
{
    public void Humanize(StyledStroke stroke, NprStrokeStyle style, int globalSeed)
    {
        if (stroke.Points.Count < 2)
        {
            return;
        }

        var medium = MediumProfile.For(style.Medium);
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
        var overshootStart = style.Overshoot * medium.OvershootScale * (0.55f + NprRandom.Float01(NprRandom.Hash(seed, 1)) * 0.65f);
        var overshootEnd = style.Overshoot * medium.OvershootScale * (0.55f + NprRandom.Float01(NprRandom.Hash(seed, 2)) * 0.65f);
        var startNormalJitter = NprRandom.SignedFloat(seed, 3) * style.EndpointJitter * medium.Noise.EndpointJitterScale;
        var endNormalJitter = NprRandom.SignedFloat(seed, 4) * style.EndpointJitter * medium.Noise.EndpointJitterScale;
        var startTangentialJitter = NprRandom.SignedFloat(seed, 5) * style.EndpointJitter * medium.Noise.TangentialJitterScale;
        var endTangentialJitter = NprRandom.SignedFloat(seed, 6) * style.EndpointJitter * medium.Noise.TangentialJitterScale;
        var midpointBend = NprRandom.SignedFloat(seed, 8) * style.EndpointJitter * medium.Noise.MidpointBendScale;

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

        var thicknessPressure = medium.Pressure.Sample(0.5f);
        stroke.Thickness = MathF.Max(
            0.35f,
            stroke.Thickness * thicknessPressure +
            NprRandom.SignedFloat(seed, 7) * style.ThicknessVariation * medium.Noise.ThicknessVariationScale);

        var opacityNoise = NprRandom.SignedFloat(seed, 9) * medium.Noise.OpacityVariationScale;
        stroke.Opacity = Math.Clamp(
            stroke.Opacity * medium.Pressure.Sample(0.5f) + opacityNoise,
            medium.OpacityFloor,
            1f);
    }
}
