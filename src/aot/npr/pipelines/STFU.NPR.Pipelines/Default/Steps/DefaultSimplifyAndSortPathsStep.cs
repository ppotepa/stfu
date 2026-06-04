using STFU.NPR.Graph;
using STFU.Strokes;

namespace STFU.NPR.Pipeline.Default.Steps;

public sealed class DefaultSimplifyAndSortPathsStep : STFU.NPR.Pipeline.INprStep
{
    public void Execute(STFU.NPR.Pipeline.NprContext context)
    {
        var epsilon = context.Settings.DefaultDrawing.PathSimplify;
        var simplified = new List<DefaultProjectedPath>(context.Graph.DefaultPaths.Count);

        foreach (var path in context.Graph.DefaultPaths)
        {
            var points = Simplify(path.Points, epsilon);
            if (points.Count > 1)
            {
                simplified.Add(path with
                {
                    Points = points,
                    Length = DefaultPathMath.PathLength(points)
                });
            }
        }

        simplified.Sort((left, right) => AverageY(left.Points).CompareTo(AverageY(right.Points)));

        context.Graph.DefaultPaths.Clear();
        for (var i = 0; i < simplified.Count; i++)
        {
            context.Graph.DefaultPaths.Add(simplified[i] with { PathIndex = i });
        }
    }

    private static IReadOnlyList<Point2D> Simplify(IReadOnlyList<Point2D> points, float epsilon)
    {
        if (epsilon <= 0f || points.Count <= 2)
        {
            return points.ToArray();
        }

        return Rdp(points).ToArray();

        List<Point2D> Rdp(IReadOnlyList<Point2D> source)
        {
            if (source.Count <= 2)
            {
                return source.ToList();
            }

            var maxDistance = -1d;
            var index = -1;

            for (var i = 1; i < source.Count - 1; i++)
            {
                var distance = PerpendicularDistance(source[i], source[0], source[^1]);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    index = i;
                }
            }

            if (maxDistance > epsilon)
            {
                var left = Rdp(source.Take(index + 1).ToArray());
                var right = Rdp(source.Skip(index).ToArray());
                left.RemoveAt(left.Count - 1);
                left.AddRange(right);
                return left;
            }

            return [source[0], source[^1]];
        }
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
        return points.Count == 0 ? 0f : points.Sum(point => point.Y) / points.Count;
    }
}
