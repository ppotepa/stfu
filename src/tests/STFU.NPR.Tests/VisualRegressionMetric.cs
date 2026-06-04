using STFU.Strokes;

internal static class VisualRegressionMetric
{
    public static float MeanEndpointDelta(StrokeFrame left, StrokeFrame right)
    {
        if (left.Paths.Count == 0 || right.Paths.Count == 0 || left.Paths.Count != right.Paths.Count)
        {
            return float.PositiveInfinity;
        }

        var total = 0f;
        var count = 0;
        for (var index = 0; index < left.Paths.Count; index++)
        {
            var a = left.Paths[index];
            var b = right.Paths[index];
            if (a.Points.Count == 0 || b.Points.Count == 0)
            {
                continue;
            }

            total += Distance(a.Points[0], b.Points[0]);
            total += Distance(a.Points[^1], b.Points[^1]);
            count += 2;
        }

        return count == 0 ? 0f : total / count;
    }

    private static float Distance(Point2D a, Point2D b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return MathF.Sqrt((dx * dx) + (dy * dy));
    }
}
