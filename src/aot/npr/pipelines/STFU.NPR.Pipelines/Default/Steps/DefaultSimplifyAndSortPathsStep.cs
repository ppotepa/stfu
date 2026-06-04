using STFU.NPR.Graph;
using STFU.Strokes;

namespace STFU.NPR.Pipeline.Default.Steps;

public sealed class DefaultSimplifyAndSortPathsStep : STFU.NPR.Pipeline.INprStep
{
    public void Execute(STFU.NPR.Pipeline.NprContext context)
    {
        var epsilon = context.Settings.DefaultDrawing.PathSimplify;
        var simplified = new List<(DefaultProjectedPath Path, float SortY)>(context.Graph.DefaultPaths.Count);

        foreach (var path in context.Graph.DefaultPaths)
        {
            var points = Simplify(path.Points, epsilon);
            if (points.Count > 1)
            {
                var length = ReferenceEquals(points, path.Points)
                    ? path.Length
                    : DefaultPathMath.PathLength(points);
                var simplifiedPath = path with
                {
                    Points = points,
                    Length = length
                };
                simplified.Add((simplifiedPath, AverageY(points)));
            }
        }

        simplified.Sort((left, right) => left.SortY.CompareTo(right.SortY));

        context.Graph.DefaultPaths.Clear();
        for (var i = 0; i < simplified.Count; i++)
        {
            context.Graph.DefaultPaths.Add(simplified[i].Path with { PathIndex = i });
        }
    }

    private static IReadOnlyList<Point2D> Simplify(IReadOnlyList<Point2D> points, float epsilon)
    {
        if (epsilon <= 0f || points.Count <= 2)
        {
            return points;
        }

        var keep = new bool[points.Count];
        keep[0] = true;
        keep[^1] = true;
        var stack = new Stack<(int Start, int End)>();
        stack.Push((0, points.Count - 1));

        while (stack.Count > 0)
        {
            var (start, end) = stack.Pop();

            var maxDistance = -1d;
            var index = -1;
            for (var i = start + 1; i < end; i++)
            {
                var distance = PerpendicularDistance(points[i], points[start], points[end]);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    index = i;
                }
            }

            if (maxDistance > epsilon)
            {
                keep[index] = true;
                stack.Push((start, index));
                stack.Push((index, end));
            }
        }

        var output = new List<Point2D>(points.Count);
        for (var i = 0; i < points.Count; i++)
        {
            if (keep[i])
            {
                output.Add(points[i]);
            }
        }

        return output;
    }

    private static double PerpendicularDistance(Point2D point, Point2D a, Point2D b)
    {
        var dx = (double)b.X - a.X;
        var dy = (double)b.Y - a.Y;
        if (dx == 0d && dy == 0d)
        {
            return DefaultPathMath.SegmentLength(point, a);
        }

        return Math.Abs(dy * point.X - dx * point.Y + (double)b.X * a.Y - (double)b.Y * a.X) /
            Math.Sqrt(dx * dx + dy * dy);
    }

    private static float AverageY(IReadOnlyList<Point2D> points)
    {
        if (points.Count == 0)
        {
            return 0f;
        }

        var total = 0f;
        for (var i = 0; i < points.Count; i++)
        {
            total += points[i].Y;
        }

        return total / points.Count;
    }
}
