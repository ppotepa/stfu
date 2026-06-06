using STFU.Common.Math;
using STFU.NPR.Graph;
using STFU.NPR.Pipeline;
using STFU.Parallelism;
using STFU.Strokes;

namespace STFU.NPR.Pipeline.Default.Steps;

public sealed class DefaultSimplifyAndSortPathsStep : INprStep
{
    private static readonly Func<Point2D, float> GetX = static point => point.X;
    private static readonly Func<Point2D, float> GetY = static point => point.Y;
    private static readonly Func<DefaultProjectedPath, IReadOnlyList<Point2D>> GetPathPoints = static path => path.Points;

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
        var inputPointCount = PathSimplificationMath.CountPoints(context.Graph.DefaultPaths, GetPathPoints);
        var simplifySkipped = PathSimplificationMath.CountSimplifySkipped(context.Graph.DefaultPaths, epsilon, GetPathPoints);
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
        context.Counters.Set("DefaultSimplifyAndSortPathsStep.outputPointCount", PathSimplificationMath.CountPoints(context.Graph.DefaultPaths, GetPathPoints));
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
            var points = PathSimplificationMath.SimplifyRamerDouglasPeucker(path.Points, epsilon, GetX, GetY, partition.Scratch);
            if (points.Count <= 1)
            {
                continue;
            }

            var length = ReferenceEquals(points, path.Points)
                ? path.Length
                : PathMath.PathLength(points, GetX, GetY);
            var simplifiedPath = path with
            {
                Points = points,
                Length = length
            };
            partition.Items.Add(new SimplifiedPathInfo(simplifiedPath, PathSimplificationMath.AverageY(points, GetY), pathIndex));
        }
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

    private readonly record struct SimplifiedPathInfo(
        DefaultProjectedPath Path,
        float SortY,
        int OriginalIndex);

    private sealed class SimplifyPartitionBuffer
    {
        public List<SimplifiedPathInfo> Items { get; } = [];

        public PathSimplificationScratch Scratch { get; } = new();

        public void Reset(int capacity)
        {
            Items.Clear();
            Items.EnsureCapacity(capacity);
        }
    }

}