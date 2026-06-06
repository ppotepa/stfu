using STFU.Common.Math;
using STFU.NPR.Graph;
using STFU.Parallelism;
using STFU.Strokes;

namespace STFU.NPR.Pipeline.Default.Steps;

public sealed class DefaultBuildPathsFromFragmentsStep : STFU.NPR.Pipeline.INprStep
{
    private const float Quantization = 2.5f;
    private readonly List<DefaultLineFragment> _silhouette = [];
    private readonly List<DefaultLineFragment> _feature = [];
    private readonly List<DefaultLineFragment> _boundary = [];
    private readonly PathBuildScratch _silhouetteScratch = new();
    private readonly PathBuildScratch _featureScratch = new();
    private readonly PathBuildScratch _boundaryScratch = new();

    public void Execute(STFU.NPR.Pipeline.NprContext context)
    {
        context.Graph.DefaultPaths.Clear();
        var inputFragmentCount = context.Graph.DefaultFragments.Count;
        context.Graph.DefaultPaths.EnsureCapacity(inputFragmentCount);

        _silhouette.Clear();
        _feature.Clear();
        _boundary.Clear();
        _silhouette.EnsureCapacity(inputFragmentCount);
        _feature.EnsureCapacity(inputFragmentCount);
        _boundary.EnsureCapacity(inputFragmentCount);

        foreach (var fragment in context.Graph.DefaultFragments)
        {
            switch (fragment.Type)
            {
                case DefaultLineKind.Silhouette:
                    _silhouette.Add(fragment);
                    break;
                case DefaultLineKind.Feature:
                    _feature.Add(fragment);
                    break;
                default:
                    _boundary.Add(fragment);
                    break;
            }
        }

        List<DefaultProjectedPath>? silhouettePaths = null;
        List<DefaultProjectedPath>? featurePaths = null;
        List<DefaultProjectedPath>? boundaryPaths = null;
        var parallel = context.WorkerCount > 1 && context.Graph.DefaultFragments.Count >= 512;

        if (parallel)
        {
            DeterministicParallel.ForRanges(
                0,
                3,
                NumericMath.AtMost(3, context.WorkerCount),
                context.CancellationToken,
                (start, end, _, cancellationToken) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    for (var i = start; i < end; i++)
                    {
                        switch (i)
                        {
                            case 0:
                                silhouettePaths = BuildPaths(_silhouette, DefaultLineKind.Silhouette, _silhouetteScratch);
                                break;
                            case 1:
                                featurePaths = BuildPaths(_feature, DefaultLineKind.Feature, _featureScratch);
                                break;
                            default:
                                boundaryPaths = BuildPaths(_boundary, DefaultLineKind.Boundary, _boundaryScratch);
                                break;
                        }
                    }
                },
                minItemsPerRange: 1);
        }
        else
        {
            silhouettePaths = BuildPaths(_silhouette, DefaultLineKind.Silhouette, _silhouetteScratch);
            featurePaths = BuildPaths(_feature, DefaultLineKind.Feature, _featureScratch);
            boundaryPaths = BuildPaths(_boundary, DefaultLineKind.Boundary, _boundaryScratch);
        }

        AppendRange(context.Graph.DefaultPaths, silhouettePaths);
        AppendRange(context.Graph.DefaultPaths, featurePaths);
        AppendRange(context.Graph.DefaultPaths, boundaryPaths);

        context.Counters.Set("DefaultBuildPathsFromFragmentsStep.inputFragments", inputFragmentCount);
        context.Counters.Set("DefaultBuildPathsFromFragmentsStep.silhouetteFragments", _silhouette.Count);
        context.Counters.Set("DefaultBuildPathsFromFragmentsStep.featureFragments", _feature.Count);
        context.Counters.Set("DefaultBuildPathsFromFragmentsStep.boundaryFragments", _boundary.Count);
        context.Counters.Set("DefaultBuildPathsFromFragmentsStep.pathsOutput", context.Graph.DefaultPaths.Count);
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

        Point2D[] Walk(int fragmentIndex, int end)
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

            return walkPoints.ToArray();
        }

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

                AddPath(output, lineKind, Walk(endpoint.FragmentIndex, endpoint.End), pathIndex++);
            }
        }

        for (var i = 0; i < fragments.Count; i++)
        {
            if (!visited[i])
            {
                AddPath(output, lineKind, Walk(i, 0), pathIndex++);
            }
        }

        return output;
    }

    private static void AddPath(List<DefaultProjectedPath> paths, DefaultLineKind lineKind, IReadOnlyList<Point2D> points, int pathIndex)
    {
        if (points.Count <= 1)
        {
            return;
        }

        var length = DefaultPointPathAdapter.PathLength(points);
        unchecked
        {
            var stableId = ((int)lineKind * 73856093) ^ (pathIndex * 19349663);
            paths.Add(new DefaultProjectedPath(stableId, lineKind, points, pathIndex, length));
        }
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

        public void Reset(int fragmentCount)
        {
            if (StartKeys.Length < fragmentCount)
            {
                StartKeys = new EndpointKey[fragmentCount];
            }

            if (EndKeys.Length < fragmentCount)
            {
                EndKeys = new EndpointKey[fragmentCount];
            }

            if (Visited.Length < fragmentCount)
            {
                Visited = new bool[fragmentCount];
            }

            Array.Clear(Visited, 0, fragmentCount);
            Adjacency.Clear();
            WalkPoints.Clear();
            Adjacency.EnsureCapacity(fragmentCount * 2);
        }
    }

    private static EndpointKey PointKey(Point2D point)
    {
        var key = Geometry2D.QuantizePoint(point.X, point.Y, Quantization);
        return new EndpointKey(key.X, key.Y);
    }
}
