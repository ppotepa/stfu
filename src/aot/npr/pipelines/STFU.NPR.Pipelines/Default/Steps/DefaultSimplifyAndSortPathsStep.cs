using STFU.Common.Math;
using STFU.NPR.Graph;
using STFU.NPR.Pipeline;
using STFU.Parallelism;
using STFU.Strokes;

namespace STFU.NPR.Pipeline.Default.Steps;

public sealed class DefaultSimplifyAndSortPathsStep : INprStep
{
    private SimplifyPartitionBuffer[] _partitions = [];
    private readonly List<SimplifiedPathInfo> _merged = [];

    public void Execute(NprContext context)
    {
        var pathCount = context.Graph.DefaultPaths.Count;
        if (pathCount == 0)
        {
            context.Graph.DefaultPaths.Clear();
            context.Counters.Set("DefaultSimplifyAndSortPathsStep.inputPathCount", 0);
            context.Counters.Set("DefaultSimplifyAndSortPathsStep.outputPathCount", 0);
            context.Counters.Set("DefaultSimplifyAndSortPathsStep.inputPointCount", 0);
            context.Counters.Set("DefaultSimplifyAndSortPathsStep.outputPointCount", 0);
            context.Counters.Set("DefaultSimplifyAndSortPathsStep.simplifySkipped", 0);
            return;
        }

        var epsilon = context.Settings.DefaultDrawing.PathSimplify;
        var inputPointCount = CountPoints(context.Graph.DefaultPaths);
        var simplifySkipped = CountSimplifySkipped(context.Graph.DefaultPaths, epsilon);
        var rangeCount = DeterministicParallel.GetRangeCount(pathCount, context.WorkerCount, minItemsPerRange: 64);
        EnsurePartitionCapacity(rangeCount);

        if (rangeCount <= 1)
        {
            SimplifyRange(context.Graph.DefaultPaths, 0, pathCount, epsilon, _partitions[0]);
        }
        else
        {
            DeterministicParallel.ForRanges(
                0,
                pathCount,
                context.WorkerCount,
                context.CancellationToken,
                (startInclusive, endExclusive, rangeIndex, cancellationToken) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    SimplifyRange(context.Graph.DefaultPaths, startInclusive, endExclusive, epsilon, _partitions[rangeIndex]);
                },
                minItemsPerRange: 64);
        }

        _merged.Clear();
        _merged.EnsureCapacity(pathCount);
        for (var partitionIndex = 0; partitionIndex < rangeCount; partitionIndex++)
        {
            var partition = _partitions[partitionIndex];
            _merged.AddRange(partition.Items);
        }

        _merged.Sort(static (left, right) =>
        {
            var compare = left.SortY.CompareTo(right.SortY);
            if (compare != 0)
            {
                return compare;
            }

            compare = left.OriginalIndex.CompareTo(right.OriginalIndex);
            if (compare != 0)
            {
                return compare;
            }

            return left.Path.StableId.CompareTo(right.Path.StableId);
        });

        context.Graph.DefaultPaths.Clear();
        for (var i = 0; i < _merged.Count; i++)
        {
            context.Graph.DefaultPaths.Add(_merged[i].Path with { PathIndex = i });
        }

        context.Counters.Set("DefaultSimplifyAndSortPathsStep.inputPathCount", pathCount);
        context.Counters.Set("DefaultSimplifyAndSortPathsStep.outputPathCount", context.Graph.DefaultPaths.Count);
        context.Counters.Set("DefaultSimplifyAndSortPathsStep.inputPointCount", inputPointCount);
        context.Counters.Set("DefaultSimplifyAndSortPathsStep.outputPointCount", CountPoints(context.Graph.DefaultPaths));
        context.Counters.Set("DefaultSimplifyAndSortPathsStep.simplifySkipped", simplifySkipped);
    }

    private static void SimplifyRange(
        IReadOnlyList<DefaultProjectedPath> paths,
        int startInclusive,
        int endExclusive,
        float epsilon,
        SimplifyPartitionBuffer partition)
    {
        partition.Reset(endExclusive - startInclusive);

        for (var pathIndex = startInclusive; pathIndex < endExclusive; pathIndex++)
        {
            var path = paths[pathIndex];
            var points = Simplify(path.Points, epsilon, partition.Scratch);
            if (points.Count <= 1)
            {
                continue;
            }

            var length = ReferenceEquals(points, path.Points)
                ? path.Length
                : DefaultPointPathAdapter.PathLength(points);
            var simplifiedPath = path with
            {
                Points = points,
                Length = length
            };
            partition.Items.Add(new SimplifiedPathInfo(simplifiedPath, AverageY(points), pathIndex));
        }
    }

    private static IReadOnlyList<Point2D> Simplify(
        IReadOnlyList<Point2D> points,
        float epsilon,
        SimplifyScratch scratch)
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
            for (var i = start + 1; i < end; i++)
            {
                var distanceSquared = Geometry2D.PerpendicularDistanceSquared(
                    points[i].X,
                    points[i].Y,
                    points[start].X,
                    points[start].Y,
                    points[end].X,
                    points[end].Y);
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

        var output = new List<Point2D>(points.Count);
        for (var i = 0; i < points.Count; i++)
        {
            if (scratch.Keep[i])
            {
                output.Add(points[i]);
            }
        }

        return output;
    }

    private void EnsurePartitionCapacity(int rangeCount)
    {
        if (_partitions.Length >= rangeCount)
        {
            return;
        }

        Array.Resize(ref _partitions, rangeCount);
        for (var i = 0; i < _partitions.Length; i++)
        {
            _partitions[i] ??= new SimplifyPartitionBuffer();
        }
    }

    private static int CountPoints(IReadOnlyList<DefaultProjectedPath> paths)
    {
        var count = 0;
        for (var i = 0; i < paths.Count; i++)
        {
            count += paths[i].Points.Count;
        }

        return count;
    }

    private static int CountSimplifySkipped(IReadOnlyList<DefaultProjectedPath> paths, float epsilon)
    {
        var count = 0;
        for (var i = 0; i < paths.Count; i++)
        {
            if (epsilon <= 0f || paths[i].Points.Count <= 2)
            {
                count++;
            }
        }

        return count;
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

    private readonly record struct SimplifiedPathInfo(
        DefaultProjectedPath Path,
        float SortY,
        int OriginalIndex);

    private sealed class SimplifyPartitionBuffer
    {
        public List<SimplifiedPathInfo> Items { get; } = [];

        public SimplifyScratch Scratch { get; } = new();

        public void Reset(int capacity)
        {
            Items.Clear();
            Items.EnsureCapacity(capacity);
        }
    }

    private sealed class SimplifyScratch
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
}