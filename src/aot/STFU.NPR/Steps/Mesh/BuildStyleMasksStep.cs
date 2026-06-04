using STFU.NPR.Graph;
using STFU.NPR.Pipeline;
using STFU.Strokes;

namespace STFU.NPR.Steps.Mesh;

public sealed class BuildStyleMasksStep : INprStep
{
    public void Execute(NprContext context)
    {
        context.Graph.StyleMasks.Clear();

        var visible = context.Graph.Vertices.Where(vertex => vertex.IsVisible).ToArray();
        if (visible.Length == 0)
        {
            context.Graph.StyleMasks.Add(BuildViewportCenterMask(context.View.Width, context.View.Height));
            return;
        }

        var minX = visible.Min(vertex => vertex.Position.X);
        var maxX = visible.Max(vertex => vertex.Position.X);
        var minY = visible.Min(vertex => vertex.Position.Y);
        var maxY = visible.Max(vertex => vertex.Position.Y);

        if (maxX - minX < 1f || maxY - minY < 1f)
        {
            context.Graph.StyleMasks.Add(BuildViewportCenterMask(context.View.Width, context.View.Height));
            return;
        }

        var insetX = (maxX - minX) * 0.18f;
        var insetY = (maxY - minY) * 0.18f;
        var polygon = new ScreenPolygon([
            new Point2D(minX + insetX, minY + insetY),
            new Point2D(maxX - insetX, minY + insetY),
            new Point2D(maxX - insetX, maxY - insetY),
            new Point2D(minX + insetX, maxY - insetY)
        ]);

        context.Graph.StyleMasks.Add(new StyleMask(
            StableId: 1,
            Name: "focus-center",
            ScreenRegions: [polygon],
            Strength: 1f,
            Role: StyleMaskRole.Focus));
    }

    private static StyleMask BuildViewportCenterMask(int width, int height)
    {
        var insetX = width * 0.2f;
        var insetY = height * 0.2f;
        return new StyleMask(
            StableId: 1,
            Name: "focus-center",
            ScreenRegions:
            [
                new ScreenPolygon([
                    new Point2D(insetX, insetY),
                    new Point2D(width - insetX, insetY),
                    new Point2D(width - insetX, height - insetY),
                    new Point2D(insetX, height - insetY)
                ])
            ],
            Strength: 1f,
            Role: StyleMaskRole.Focus);
    }
}
