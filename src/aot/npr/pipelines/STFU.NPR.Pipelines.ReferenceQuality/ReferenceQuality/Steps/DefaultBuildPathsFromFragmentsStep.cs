using STFU.Common.Math;
using STFU.NPR.Graph;
using STFU.Parallelism;
using STFU.Strokes;

namespace STFU.NPR.Pipeline.ReferenceQuality.Steps;

public sealed class DefaultBuildPathsFromFragmentsStep : STFU.NPR.Pipeline.INprStep
{
    private static readonly Func<Point2D, float> GetX = static point => point.X;
    private static readonly Func<Point2D, float> GetY = static point => point.Y;

    private const float Quantization = 2.5f;
    private readonly List<DefaultLineFragment> _silhouetteFragments = [];
    private readonly List<DefaultLineFragment> _featureFragments = [];
    private readonly List<DefaultLineFragment> _boundaryFragments = [];
    private readonly PathBuildScratch _silhouetteScratch = new();
    private readonly PathBuildScratch _featureScratch = new();
    private readonly PathBuildScratch _boundaryScratch = new();
    private int _lastSilhouetteFragmentCount;
    private int _lastFeatureFragmentCount;
    private int _lastBoundaryFragmentCount;

    public void Execute(STFU.NPR.Pipeline.NprContext context)
    {
        context.Graph.DefaultPaths.Clear();
        var inputFragmentCount = context.Graph.DefaultFragments.Count;
        context.Graph.DefaultPaths.EnsureCapacity(inputFragmentCount);

        if (inputFragmentCount == 0)
        {
            context.Counters.Set("DefaultBuildPathsFromFragmentsStep.fragmentsInput", 0);
            context.Counters.Set("DefaultBuildPathsFromFragmentsStep.silhouetteFragments", 0);
            context.Counters.Set("DefaultBuildPathsFromFragmentsStep.featureFragments", 0);
            context.Counters.Set("DefaultBuildPathsFromFragmentsStep.boundaryFragments", 0);
            context.Counters.Set("DefaultBuildPathsFromFragmentsStep.pathsOutput", 0);
            context.Counters.Set("DefaultBuildPathsFromFragmentsStep.parallelBuild", 0);
            context.Counters.Set("DefaultBuildPathsFromFragmentsStep.expectedKindCapacity", 0);
            return;
        }

        _silhouetteFragments.Clear();
        _featureFragments.Clear();
        _boundaryFragments.Clear();
        _silhouetteFragments.EnsureCapacity(_lastSilhouetteFragmentCount);
        _featureFragments.EnsureCapacity(_lastFeatureFragmentCount);
        _boundaryFragments.EnsureCapacity(_lastBoundaryFragmentCount);

        for (var i = 0; i < context.Graph.DefaultFragments.Count; i++)
        {
            var fragment = context.Graph.DefaultFragments[i];
            switch (fragment.Type)
            {
                case DefaultLineKind.Silhouette:
                    _silhouetteFragments.Add(fragment);
                    break;
                case DefaultLineKind.Feature:
                    _featureFragments.Add(fragment);
                    break;
                default:
                    _boundaryFragments.Add(fragment);
                    break;
            }
        }

        _lastSilhouetteFragmentCount = _silhouetteFragments.Count;
        _lastFeatureFragmentCount = _featureFragments.Count;
        _lastBoundaryFragmentCount = _boundaryFragments.Count;

        List<DefaultProjectedPath>? silhouettePaths = null;
        List<DefaultProjectedPath>? featurePaths = null;
        List<DefaultProjectedPath>? boundaryPaths = null;
        context.Counters.Set("DefaultBuildPathsFromFragmentsStep.expectedKindCapacity", Math.Max(4, inputFragmentCount / 3));

        var parallel = context.WorkerCount > 1 && context.Graph.DefaultFragments.Count >= 512;

        if (parallel)
        {
            NprParallelTrace.ForRanges(
                context,
                "DefaultBuildPathsFromFragmentsStep.BuildKinds",
                0,
                3,
                (start, end, _, cancellationToken) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    for (var i = start; i < end; i++)
                    {
                        switch (i)
                        {
                            case 0:
                                silhouettePaths = _silhouetteFragments.Count == 0
                                    ? []
                                    : BuildPaths(_silhouetteFragments, DefaultLineKind.Silhouette, _silhouetteScratch);
                                break;
                            case 1:
                                featurePaths = _featureFragments.Count == 0
                                    ? []
                                    : BuildPaths(_featureFragments, DefaultLineKind.Feature, _featureScratch);
                                break;
                            default:
                                boundaryPaths = _boundaryFragments.Count == 0
                                    ? []
                                    : BuildPaths(_boundaryFragments, DefaultLineKind.Boundary, _boundaryScratch);
                                break;
                        }
                    }
                },
                minItemsPerRange: 1);
        }
        else
        {
            silhouettePaths = _silhouetteFragments.Count == 0
                ? []
                : BuildPaths(_silhouetteFragments, DefaultLineKind.Silhouette, _silhouetteScratch);
            featurePaths = _featureFragments.Count == 0
                ? []
                : BuildPaths(_featureFragments, DefaultLineKind.Feature, _featureScratch);
            boundaryPaths = _boundaryFragments.Count == 0
                ? []
                : BuildPaths(_boundaryFragments, DefaultLineKind.Boundary, _boundaryScratch);
        }

        AppendRange(context.Graph.DefaultPaths, silhouettePaths);
        AppendRange(context.Graph.DefaultPaths, featurePaths);
        AppendRange(context.Graph.DefaultPaths, boundaryPaths);

        context.Counters.Set("DefaultBuildPathsFromFragmentsStep.fragmentsInput", inputFragmentCount);
        context.Counters.Set("DefaultBuildPathsFromFragmentsStep.silhouetteFragments", _silhouetteFragments.Count);
        context.Counters.Set("DefaultBuildPathsFromFragmentsStep.featureFragments", _featureFragments.Count);
        context.Counters.Set("DefaultBuildPathsFromFragmentsStep.boundaryFragments", _boundaryFragments.Count);
        context.Counters.Set("DefaultBuildPathsFromFragmentsStep.pathsOutput", context.Graph.DefaultPaths.Count);
        context.Counters.Set("DefaultBuildPathsFromFragmentsStep.parallelBuild", parallel ? 1 : 0);
        context.Counters.Set("DefaultBuildPathsFromFragmentsStep.walkPointCopies",
            _silhouetteScratch.WalkPointCopies + _featureScratch.WalkPointCopies + _boundaryScratch.WalkPointCopies);
        context.Counters.Set("DefaultBuildPathsFromFragmentsStep.maxWalkPointCount",
            NumericMath.AtLeast(_silhouetteScratch.MaxWalkPointCount, NumericMath.AtLeast(_featureScratch.MaxWalkPointCount, _boundaryScratch.MaxWalkPointCount)));
    }

    private static void AppendRange(List<DefaultProjectedPath> output, List<DefaultProjectedPath>? paths)
    {
        if (paths is { Count: > 0 })
        {
            output.AddRange(paths);
        }
    }

    private static List<DefaultProjectedPath> BuildPaths(
        IReadOnlyList<DefaultLineFragment> fragments,
        DefaultLineKind lineKind,
        PathBuildScratch scratch)
    {
        var output = new List<DefaultProjectedPath>(NumericMath.AtLeast(fragments.Count, 4));
        if (fragments.Count == 0)
        {
            return output;
        }

        scratch.Reset(fragments.Count);
        var adjacency = scratch.Adjacency;
        var startKeys = scratch.StartKeys;
        var endKeys = scratch.EndKeys;
        var visited = scratch.Visited;
        var walkPoints = scratch.WalkPoints;

        for (var i = 0; i < fragments.Count; i++)
        {
            startKeys[i] = PointKey(fragments[i].P0);
            endKeys[i] = PointKey(fragments[i].P1);
            Add(adjacency, startKeys[i], new EndpointRef(i, 0));
            Add(adjacency, endKeys[i], new EndpointRef(i, 1));
        }

        var pathIndex = 0;

        int Walk(int fragmentIndex, int end, List<Point2D> output)
        {
            walkPoints.Clear();
            var currentFragment = fragmentIndex;
            var currentEnd = end;
            var guard = 0;

            while (currentFragment >= 0 && !visited[currentFragment] && guard++ < 20000)
            {
                visited[currentFragment] = true;
                var fragment = fragments[currentFragment];
                var first = currentEnd == 0 ? fragment.P0 : fragment.P1;
                var second = currentEnd == 0 ? fragment.P1 : fragment.P0;

                if (walkPoints.Count == 0)
                {
                    walkPoints.Add(first);
                }

                walkPoints.Add(second);

                if (!TryGetNextEndpoint(
                    adjacency,
                    currentEnd == 0 ? endKeys[currentFragment] : startKeys[currentFragment],
                    visited,
                    out var next))
                {
                    break;
                }

                currentFragment = next.FragmentIndex;
                currentEnd = next.End;
            }

            scratch.WalkPointCopies++;
            scratch.MaxWalkPointCount = NumericMath.AtLeast(scratch.MaxWalkPointCount, walkPoints.Count);
            output.Clear();
            if (walkPoints.Count == 0)
            {
                return 0;
            }

            output.EnsureCapacity(walkPoints.Count);
            for (var i = 0; i < walkPoints.Count; i++)
            {
                output.Add(walkPoints[i]);
            }

            return walkPoints.Count;
        }

        var pathPoints = new List<Point2D>();
        foreach (var pair in adjacency)
        {
            if (pair.Value.Count == 2)
            {
                continue;
            }

            foreach (var endpoint in pair.Value)
            {
                if (visited[endpoint.FragmentIndex])
                {
                    continue;
                }

                var pathLength = Walk(endpoint.FragmentIndex, endpoint.End, pathPoints);
                AddPath(output, lineKind, pathPoints, pathLength, pathIndex++);
            }
        }

        for (var i = 0; i < fragments.Count; i++)
        {
            if (!visited[i])
            {
                var pathLength = Walk(i, 0, pathPoints);
                AddPath(output, lineKind, pathPoints, pathLength, pathIndex++);
            }
        }

        return output;
    }

    private static void AddPath(
        List<DefaultProjectedPath> paths,
        DefaultLineKind lineKind,
        List<Point2D> points,
        int pathPointCount,
        int pathIndex)
    {
        if (pathPointCount <= 1)
        {
            return;
        }

        var length = CalculatePathLength(points, pathPointCount, GetX, GetY);
        unchecked
        {
            var stableId = ((int)lineKind * 73856093) ^ (pathIndex * 19349663);
            paths.Add(new DefaultProjectedPath(stableId, lineKind, points.GetRange(0, pathPointCount), pathIndex, length));
        }
    }

    private static float CalculatePathLength(List<Point2D> points, int pointCount, Func<Point2D, float> getX, Func<Point2D, float> getY)
    {
        if (pointCount <= 1)
        {
            return 0f;
        }

        var length = 0f;
        for (var i = 0; i < pointCount - 1; i++)
        {
            length += PathMath.SegmentLength(points[i], points[i + 1], getX, getY);
        }

        return length;
    }

    private static bool TryGetNextEndpoint(
        Dictionary<EndpointKey, EndpointBucket> adjacency,
        EndpointKey key,
        bool[] visited,
        out EndpointRef next)
    {
        if (adjacency.TryGetValue(key, out var refs))
        {
            foreach (var candidate in refs)
            {
                if (!visited[candidate.FragmentIndex])
                {
                    next = candidate;
                    return true;
                }
            }
        }

        next = default;
        return false;
    }

    private static void Add(Dictionary<EndpointKey, EndpointBucket> adjacency, EndpointKey key, EndpointRef value)
    {
        adjacency.TryGetValue(key, out var bucket);
        bucket.Add(value);
        adjacency[key] = bucket;
    }

    private struct EndpointBucket
    {
        private EndpointRef _first;
        private EndpointRef _second;
        private List<EndpointRef>? _overflow;

        public int Count { get; private set; }

        public readonly EndpointRef this[int index]
        {
            get
            {
                return index switch
                {
                    0 => _first,
                    1 => _second,
                    _ => _overflow![index - 2]
                };
            }
        }

        public void Add(EndpointRef value)
        {
            switch (Count)
            {
                case 0:
                    _first = value;
                    break;
                case 1:
                    _second = value;
                    break;
                default:
                    _overflow ??= [];
                    _overflow.Add(value);
                    break;
            }

            Count++;
        }

        public readonly Enumerator GetEnumerator() => new(this);

        public struct Enumerator(EndpointBucket bucket)
        {
            private readonly EndpointBucket _bucket = bucket;
            private int _index = -1;

            public EndpointRef Current => _bucket[_index];

            public bool MoveNext()
            {
                _index++;
                return _index < _bucket.Count;
            }
        }
    }

    private readonly record struct EndpointRef(int FragmentIndex, int End);

    private readonly record struct EndpointKey(int X, int Y);

    private sealed class PathBuildScratch
    {
        public EndpointKey[] StartKeys = [];
        public EndpointKey[] EndKeys = [];
        public bool[] Visited = [];
        public readonly Dictionary<EndpointKey, EndpointBucket> Adjacency = new();
        public readonly List<Point2D> WalkPoints = new(32);
        public int WalkPointCopies;
        public int MaxWalkPointCount;

        public void Reset(int fragmentCount)
        {
            if (StartKeys.Length < fragmentCount)
            {
                StartKeys = new EndpointKey[GrowCapacity(fragmentCount)];
            }

            if (EndKeys.Length < fragmentCount)
            {
                EndKeys = new EndpointKey[GrowCapacity(fragmentCount)];
            }

            if (Visited.Length < fragmentCount)
            {
                Visited = new bool[GrowCapacity(fragmentCount)];
            }

            Array.Clear(Visited, 0, fragmentCount);
            Adjacency.Clear();
            WalkPoints.Clear();
            WalkPointCopies = 0;
            MaxWalkPointCount = 0;
            Adjacency.EnsureCapacity(fragmentCount * 2);
        }

        private static int GrowCapacity(int required)
        {
            var capacity = 4;
            while (capacity < required)
            {
                capacity = checked(capacity + (capacity >> 1));
            }

            return capacity;
        }
    }

    private static EndpointKey PointKey(Point2D point)
    {
        var key = Geometry2D.QuantizePoint(point.X, point.Y, Quantization);
        return new EndpointKey(key.X, key.Y);
    }
}
