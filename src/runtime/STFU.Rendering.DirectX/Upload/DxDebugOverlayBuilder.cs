using STFU.Common.Math;
using STFU.NPR.Debug;
using STFU.Strokes;

namespace STFU.Rendering.DirectX.Upload;

public static class DxDebugOverlayBuilder
{
    public static List<StrokePath2D> Build(NprDebugFrame debugFrame, DebugOverlayKind overlay)
    {
        if (overlay == DebugOverlayKind.None || debugFrame.Lines.Count == 0)
        {
            return [];
        }

        var paths = new List<StrokePath2D>(debugFrame.Lines.Count);
        foreach (var line in debugFrame.Lines)
        {
            if (line.Kind != overlay)
            {
                continue;
            }

            var (color, thickness) = ResolveStyle(line, overlay);
            var style = new StrokeStyle2D(thickness, color.opacity, color.color);
            paths.Add(StrokePath2D.Line(line.Start, line.End, style));
        }

        return paths;
    }

    private static ((StrokeColor color, float opacity) color, float thickness) ResolveStyle(NprDebugLine line, DebugOverlayKind overlay)
    {
        return overlay switch
        {
            DebugOverlayKind.FeatureCurves => line.Label switch
            {
                "Boundary" => ((new StrokeColor(215, 110, 40), 220f / 255f), 1.5f),
                "Silhouette" => ((new StrokeColor(35, 35, 35), 235f / 255f), 1.5f),
                "Crease" => ((new StrokeColor(35, 110, 210), 220f / 255f), 1.5f),
                "SurfaceFlow" => ((new StrokeColor(55, 155, 105), 220f / 255f), 1.5f),
                "Hatch" => ((new StrokeColor(135, 70, 160), 190f / 255f), 1.5f),
                _ => ((new StrokeColor(90, 90, 90), 210f / 255f), 1.5f)
            },
            DebugOverlayKind.VisibilitySegments => line.IsPrimary
                ? ((new StrokeColor(30, 150, 80), 220f / 255f), 1.25f)
                : ((new StrokeColor(190, 60, 60), 200f / 255f), 1.25f),
            DebugOverlayKind.SalienceHeatmap => (ColorFromHeat(line.Value, (byte)(line.IsPrimary ? 235 : 155)), 2.0f),
            DebugOverlayKind.StrokeCandidates => line.Label switch
            {
                "Silhouette" => ((new StrokeColor(25, 25, 25), 235f / 255f), 1.6f),
                "Boundary" => ((new StrokeColor(30, 30, 30), 220f / 255f), 1.6f),
                "Crease" => ((new StrokeColor(45, 120, 220), 220f / 255f), 1.6f),
                "SurfaceFlow" => ((new StrokeColor(70, 170, 110), 210f / 255f), 1.6f),
                "Hatch" => ((new StrokeColor(145, 80, 170), 200f / 255f), 1.6f),
                _ => ((new StrokeColor(90, 90, 90), 210f / 255f), 1.6f)
            },
            DebugOverlayKind.ToneField => (ColorFromHeat(line.Value, 210), 1.4f),
            DebugOverlayKind.DirectionField => ((new StrokeColor(50, 120, 220), 210f / 255f), 1.25f),
            DebugOverlayKind.DensityField => ((new StrokeColor(145, 85, 195), 210f / 255f), 1.5f),
            DebugOverlayKind.TextureField => ((new StrokeColor(180, 120, 55), 210f / 255f), 1.35f),
            DebugOverlayKind.TemporalMatches => line.IsPrimary
                ? ((new StrokeColor(35, 175, 215), 220f / 255f), 1.7f)
                : ((new StrokeColor(215, 125, 35), 220f / 255f), 1.45f),
            DebugOverlayKind.GhostStrokes => ((new StrokeColor(150, 150, 150), 180f / 255f), 1.3f),
            DebugOverlayKind.HatchingPlan => line.Label switch
            {
                "Primary" => ((new StrokeColor(125, 70, 165), 220f / 255f), 1.8f),
                "Cross" => ((new StrokeColor(70, 135, 195), 220f / 255f), 1.35f),
                "Tertiary" => ((new StrokeColor(55, 110, 125), 220f / 255f), 1.35f),
                _ => ((new StrokeColor(120, 120, 120), 210f / 255f), 1.35f)
            },
            DebugOverlayKind.StyleMask => ((new StrokeColor(230, 130, 35), 220f / 255f), 2.0f),
            DebugOverlayKind.MaterialRegion => ((new StrokeColor(55, 155, 210), 220f / 255f), 1.7f),
            _ => ((new StrokeColor(90, 90, 90), 200f / 255f), 1.25f)
        };
    }

    private static (StrokeColor color, float opacity) ColorFromHeat(float value, byte alpha)
    {
        var (red, green, blue) = ColorMath.HeatRgb(value);
        return (new StrokeColor(red, green, blue), alpha / 255f);
    }
}
