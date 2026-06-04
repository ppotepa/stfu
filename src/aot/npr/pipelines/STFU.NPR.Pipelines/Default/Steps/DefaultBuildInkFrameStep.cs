using STFU.NPR.Composition;
using STFU.NPR.Graph;
using STFU.NPR.Rendering;
using STFU.Strokes;

namespace STFU.NPR.Pipeline.Default.Steps;

public sealed class DefaultBuildInkFrameStep : STFU.NPR.Pipeline.INprStep
{
    public void Execute(STFU.NPR.Pipeline.NprContext context)
    {
        var paths = new List<StrokePath2D>();

        foreach (var path in context.Graph.DefaultDrawablePaths)
        {
            AddStyledPath(context, path, paths);
        }

        context.Frame = new StrokeFrame(context.Width, context.Height, paths);
        context.NprFrame = new NprFrame(
            context.Width,
            context.Height,
            new NprPaper(context.Settings.DefaultDrawing.PaperColor, 1f),
            BuildLayers(paths),
            context.Frame);
    }

    private static void AddStyledPath(STFU.NPR.Pipeline.NprContext context, DefaultProjectedPath path, List<StrokePath2D> output)
    {
        if (path.Points.Count < 2)
        {
            return;
        }

        var drawing = context.Settings.DefaultDrawing;
        var intent = ToIntent(path.Type);
        var curveKind = ToCurveKind(path.Type);
        var layerName = context.Style.ResolveOutputLayer(curveKind, intent, VisibilityState.Visible);
        var layerOrder = context.Style.GetLayerOrder(curveKind, intent, VisibilityState.Visible);
        var profile = context.Style.Stroke.FindProfile(curveKind, intent, layerName) ?? context.Style.Stroke.FindProfile(intent);
        var strokeColor = profile?.Color ?? drawing.StrokeColor;
        var baseWidth = Math.Max(0.35f, (profile?.BaseThickness ?? drawing.LineWidth) * context.Style.Stroke.ThicknessScale);
        var baseOpacity = Math.Clamp((profile?.BaseOpacity ?? 1f) * context.Style.Stroke.OpacityScale, 0f, 1f);
        var style = drawing.StrokeStyle;
        var comic = style == STFU.NPR.Settings.DefaultStrokeStyle.ComicInk;
        var baseJitter = drawing.Jitter * (path.Type == DefaultLineKind.Feature ? 0.8f : 1f);
        var pressure = comic ? Math.Max((double)drawing.Pressure, 0.54d) : drawing.Pressure;
        var seed = context.Settings.Seed + path.PathIndex * 17;
        var passes = style switch
        {
            STFU.NPR.Settings.DefaultStrokeStyle.Pencil => 3,
            STFU.NPR.Settings.DefaultStrokeStyle.Brush => 2,
            STFU.NPR.Settings.DefaultStrokeStyle.ComicInk => 2,
            _ => 1
        };

        for (var pass = 0; pass < passes; pass++)
        {
            var alpha = style switch
            {
                STFU.NPR.Settings.DefaultStrokeStyle.Pencil => 0.18f,
                STFU.NPR.Settings.DefaultStrokeStyle.Brush => pass == 0 ? 0.28f : 0.75f,
                STFU.NPR.Settings.DefaultStrokeStyle.ComicInk => pass == 0 ? 0.98f : 0.34f,
                _ => 0.92f
            };

            var passJitter = style switch
            {
                STFU.NPR.Settings.DefaultStrokeStyle.Pencil => baseJitter * (1f + pass * 0.55f),
                STFU.NPR.Settings.DefaultStrokeStyle.Brush => baseJitter * (pass == 0 ? 1.1f : 0.35f),
                STFU.NPR.Settings.DefaultStrokeStyle.ComicInk => baseJitter * (pass == 0 ? 0.16f : 0.46f),
                _ => baseJitter * 0.35f
            };

            var widthMultiplier = style switch
            {
                STFU.NPR.Settings.DefaultStrokeStyle.Pencil => 0.9f,
                STFU.NPR.Settings.DefaultStrokeStyle.Brush => pass == 0 ? 1.6f : 0.85f,
                STFU.NPR.Settings.DefaultStrokeStyle.ComicInk => pass == 0 ? 1.10f : 0.54f,
                _ => 0.75f
            };

            for (var i = 1; i < path.Points.Count; i++)
            {
                var start = JitterPoint(path.Points, i - 1, passJitter, seed + pass * 11, drawing.EnableFastNoise);
                var end = JitterPoint(path.Points, i, passJitter, seed + pass * 11 + 3, drawing.EnableFastNoise);
                var t = i / (double)Math.Max(1, path.Points.Count - 1);
                var pressureNoise = 1d + pressure *
                    ((DefaultNoise.Noise01(t * 9d + pass * 1.7d, seed, drawing.EnableFastNoise) - 0.5d) *
                        (comic ? 1.25d : 1.7d));
                var taper = comic
                    ? 0.82d + 0.30d * Math.Sin(Math.PI * t)
                    : 1d;
                var lineWidth = Math.Max(0.35d, baseWidth * widthMultiplier * pressureNoise * taper);

                if (style == STFU.NPR.Settings.DefaultStrokeStyle.Pencil &&
                    DefaultNoise.Noise01(i * 2.13d + pass, seed, drawing.EnableFastNoise) < 0.06d)
                {
                    continue;
                }

                if (comic && pass == 1 &&
                    DefaultNoise.Noise01(i * 3.81d, seed, drawing.EnableFastNoise) < 0.26d)
                {
                    continue;
                }

                var segmentStyle = new StrokeStyle2D((float)lineWidth, Math.Clamp(alpha * baseOpacity, 0f, 1f), strokeColor);
                output.Add(new StrokePath2D(
                    [start, end],
                    segmentStyle,
                    [
                        StrokePoint2D.FromPoint(start, segmentStyle),
                        StrokePoint2D.FromPoint(end, segmentStyle)
                    ],
                    new StrokeMetadata(
                        HashStroke(path.StableId, pass, i),
                        layerName,
                        "DefaultInkSegment",
                        intent.ToString(),
                        path.StableId,
                        i,
                        "Visible",
                        context.Style.StyleId,
                        null,
                        layerOrder)));
            }
        }
    }

    private static IReadOnlyList<NprLayerFrame> BuildLayers(IReadOnlyList<StrokePath2D> paths)
    {
        return paths
            .GroupBy(path => path.Metadata?.Layer ?? "default")
            .Select(group => new NprLayerFrame(
                group.Key,
                LayerTitle(group.Key),
                NprSceneRole.Foreground,
                group.First().Metadata?.LayerOrder ?? 100,
                true,
                1f,
                NprLayerBlendMode.Normal,
                [],
                [],
                group.ToArray()))
            .OrderBy(layer => layer.Order)
            .ToArray();
    }

    private static string LayerTitle(string layer)
    {
        return layer switch
        {
            "silhouette" => "Silhouette",
            "feature" => "Feature",
            "boundary" => "Boundary",
            _ => layer
        };
    }

    private static Point2D JitterPoint(IReadOnlyList<Point2D> points, int index, float amount, int seed, bool fastNoise)
    {
        var point = points[index];
        var previous = points[Math.Max(0, index - 1)];
        var next = points[Math.Min(points.Count - 1, index + 1)];
        var tx = next.X - previous.X;
        var ty = next.Y - previous.Y;
        var length = MathF.Sqrt(tx * tx + ty * ty);
        if (length <= 1e-5f)
        {
            length = 1f;
        }

        var nx = -ty / length;
        var ny = tx / length;
        var magnitude = (float)((DefaultNoise.Noise01(index * 1.371d + point.X * 0.003d + point.Y * 0.002d, seed, fastNoise) - 0.5d) * 2d * amount);

        return new Point2D(
            point.X + nx * magnitude,
            point.Y + ny * magnitude);
    }

    private static NprStrokeIntent ToIntent(DefaultLineKind kind)
    {
        return kind switch
        {
            DefaultLineKind.Silhouette => NprStrokeIntent.Silhouette,
            DefaultLineKind.Feature => NprStrokeIntent.Crease,
            _ => NprStrokeIntent.Boundary
        };
    }

    private static FeatureCurveKind ToCurveKind(DefaultLineKind kind)
    {
        return kind switch
        {
            DefaultLineKind.Silhouette => FeatureCurveKind.Silhouette,
            DefaultLineKind.Feature => FeatureCurveKind.Crease,
            _ => FeatureCurveKind.Boundary
        };
    }

    private static int HashStroke(int pathId, int pass, int segmentIndex)
    {
        unchecked
        {
            return pathId * 397 ^ pass * 97 ^ segmentIndex;
        }
    }
}
