using STFU.NPR.Graph;
using STFU.NPR.Pipeline;
using STFU.Strokes;

namespace STFU.NPR.Steps.Strokes;

public sealed class StyleStrokesStep : INprStep
{
    public void Execute(NprContext context)
    {
        foreach (var stroke in context.Graph.Strokes)
        {
            var depthFactor = 1f / (1f + MathF.Max(0f, stroke.Depth) * 0.16f);
            var lengthFactor = Math.Clamp(stroke.ScreenLength / 180f, 0f, 1f);
            var shadeBoost = Math.Clamp(stroke.Shade, 0f, 1f);

            var (baseThickness, baseOpacity, color) = stroke.Intent switch
            {
                NprStrokeIntent.Silhouette => (2.1f, 0.95f, new StrokeColor(12, 12, 12)),
                NprStrokeIntent.Boundary => (1.8f, 0.85f, new StrokeColor(18, 18, 18)),
                NprStrokeIntent.Crease => (1.55f, 0.72f, new StrokeColor(28, 28, 26)),
                NprStrokeIntent.SurfaceFlow => (0.75f, 0.22f + shadeBoost * 0.26f, new StrokeColor(62, 62, 58)),
                NprStrokeIntent.Hatch => (0.65f, 0.35f, new StrokeColor(48, 48, 45)),
                _ => (1.2f, 0.65f, StrokeColor.Black)
            };

            stroke.Thickness = MathF.Max(0.35f, baseThickness * (0.72f + depthFactor * 0.34f + lengthFactor * 0.12f));
            stroke.Opacity = Math.Clamp(baseOpacity * (0.55f + depthFactor * 0.45f) * (0.85f + stroke.Importance * 0.2f), 0.06f, 1f);
            stroke.Color = color;
        }
    }
}
