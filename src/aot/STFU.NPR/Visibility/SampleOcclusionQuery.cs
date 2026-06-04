using STFU.NPR.Pipeline;
using STFU.Strokes;

namespace STFU.NPR.Visibility;

public sealed class SampleOcclusionQuery : IOcclusionQuery
{
    public bool IsOccluded(NprContext context, Point2D point, float lineDepth)
    {
        foreach (var triangle in context.Graph.Triangles)
        {
            if (!triangle.IsVisible ||
                !triangle.IsFrontFacing ||
                triangle.Depth >= lineDepth - context.Settings.HiddenLineDepthBias)
            {
                continue;
            }

            var a = context.Graph.Vertices[triangle.A].Position;
            var b = context.Graph.Vertices[triangle.B].Position;
            var c = context.Graph.Vertices[triangle.C].Position;

            if (ContainsPoint(point, a, b, c))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsPoint(Point2D p, Point2D a, Point2D b, Point2D c)
    {
        var d1 = Sign(p, a, b);
        var d2 = Sign(p, b, c);
        var d3 = Sign(p, c, a);
        var hasNegative = d1 < 0 || d2 < 0 || d3 < 0;
        var hasPositive = d1 > 0 || d2 > 0 || d3 > 0;
        return !(hasNegative && hasPositive);
    }

    private static float Sign(Point2D p1, Point2D p2, Point2D p3)
    {
        return (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
    }
}
