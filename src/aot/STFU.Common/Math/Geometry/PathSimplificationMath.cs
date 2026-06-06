namespace STFU.Common.Math;

public sealed class PathSimplificationScratch
{
    public bool[] Keep = [];
    public int[] StackStart = [];
    public int[] StackEnd = [];

    public void EnsureCapacity(int pointCount)
    {
        if (Keep.Length < pointCount)
        {
            Keep = new bool[pointCount];
        }

        var stackCapacity = NumericMath.AtLeast(pointCount, 4);
        if (StackStart.Length < stackCapacity)
        {
            StackStart = new int[stackCapacity];
            StackEnd = new int[stackCapacity];
        }
    }

    public void ClearKeep(int pointCount)
    {
        Array.Clear(Keep, 0, pointCount);
    }
}

public static class PathSimplificationMath
{
    public static IReadOnlyList<TPoint> SimplifyRamerDouglasPeucker<TPoint>(
        IReadOnlyList<TPoint> points,
        float epsilon,
        Func<TPoint, float> getX,
        Func<TPoint, float> getY,
        PathSimplificationScratch scratch)
    {
        if (epsilon <= 0f || points.Count <= 2)
        {
            return points;
        }

        scratch.EnsureCapacity(points.Count);
        scratch.ClearKeep(points.Count);
        scratch.Keep[0] = true;
        scratch.Keep[points.Count - 1] = true;
        var epsilonSquared = (double)epsilon * epsilon;

        var stackCount = 0;
        scratch.StackStart[stackCount] = 0;
        scratch.StackEnd[stackCount] = points.Count - 1;
        stackCount++;

        while (stackCount > 0)
        {
            stackCount--;
            var start = scratch.StackStart[stackCount];
            var end = scratch.StackEnd[stackCount];

            var maxDistanceSquared = -1d;
            var index = -1;
            var startPoint = points[start];
            var endPoint = points[end];
            var ax = getX(startPoint);
            var ay = getY(startPoint);
            var bx = getX(endPoint);
            var by = getY(endPoint);

            for (var i = start + 1; i < end; i++)
            {
                var point = points[i];
                var distanceSquared = Geometry2D.PerpendicularDistanceSquared(getX(point), getY(point), ax, ay, bx, by);
                if (distanceSquared > maxDistanceSquared)
                {
                    maxDistanceSquared = distanceSquared;
                    index = i;
                }
            }

            if (maxDistanceSquared > epsilonSquared)
            {
                scratch.Keep[index] = true;
                scratch.StackStart[stackCount] = start;
                scratch.StackEnd[stackCount] = index;
                stackCount++;
                scratch.StackStart[stackCount] = index;
                scratch.StackEnd[stackCount] = end;
                stackCount++;
            }
        }

        var output = new List<TPoint>(points.Count);
        for (var i = 0; i < points.Count; i++)
        {
            if (scratch.Keep[i])
            {
                output.Add(points[i]);
            }
        }

        return output;
    }

    public static int CountPoints<TPath, TPoint>(IReadOnlyList<TPath> paths, Func<TPath, IReadOnlyList<TPoint>> getPoints)
    {
        var count = 0;
        for (var i = 0; i < paths.Count; i++)
        {
            count += getPoints(paths[i]).Count;
        }

        return count;
    }

    public static int CountSimplifySkipped<TPath, TPoint>(
        IReadOnlyList<TPath> paths,
        float epsilon,
        Func<TPath, IReadOnlyList<TPoint>> getPoints)
    {
        var count = 0;
        for (var i = 0; i < paths.Count; i++)
        {
            if (epsilon <= 0f || getPoints(paths[i]).Count <= 2)
            {
                count++;
            }
        }

        return count;
    }

    public static float AverageY<TPoint>(IReadOnlyList<TPoint> points, Func<TPoint, float> getY)
    {
        if (points.Count == 0)
        {
            return 0f;
        }

        var total = 0f;
        for (var i = 0; i < points.Count; i++)
        {
            total += getY(points[i]);
        }

        return total / points.Count;
    }
}
