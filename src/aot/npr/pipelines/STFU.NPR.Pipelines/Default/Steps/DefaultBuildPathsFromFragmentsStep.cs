using STFU.NPR.Graph;
using STFU.Strokes;

namespace STFU.NPR.Pipeline.Default.Steps;

public sealed class DefaultBuildPathsFromFragmentsStep : STFU.NPR.Pipeline.INprStep
{
    private const float Quantization = 2.5f;
    private readonly List<DefaultLineFragment> _silhouette = [];
    private readonly List<DefaultLineFragment> _feature = [];
    private readonly List<DefaultLineFragment> _boundary = [];
    private readonly Dictionary<EndpointKey, EndpointBucket> _adjacency = new();
    private EndpointKey[] _startKeys = [];
    private EndpointKey[] _endKeys = [];
    private bool[] _visited = [];

    public void Execute(STFU.NPR.Pipeline.NprContext context)
    {
        context.Graph.DefaultPaths.Clear();
        context.Graph.DefaultPaths.EnsureCapacity(context.Graph.DefaultFragments.Count);

        var silhouetteCount = 0;
        var featureCount = 0;
        var boundaryCount = 0;
        foreach (var fragment in context.Graph.DefaultFragments)
        {
            switch (fragment.Type)
            {
                case DefaultLineKind.Silhouette:
                    silhouetteCount++;
                    break;
                case DefaultLineKind.Feature:
                    featureCount++;
                    break;
                default:
                    boundaryCount++;
                    break;
            }
        }

        _silhouette.Clear();
        _feature.Clear();
        _boundary.Clear();
        _silhouette.EnsureCapacity(silhouetteCount);
        _feature.EnsureCapacity(featureCount);
        _boundary.EnsureCapacity(boundaryCount);

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

        AppendPaths(context.Graph.DefaultPaths, _silhouette, DefaultLineKind.Silhouette);
        AppendPaths(context.Graph.DefaultPaths, _feature, DefaultLineKind.Feature);
        AppendPaths(context.Graph.DefaultPaths, _boundary, DefaultLineKind.Boundary);
    }

    private void AppendPaths(
        List<DefaultProjectedPath> output,
        IReadOnlyList<DefaultLineFragment> fragments,
        DefaultLineKind lineKind)
    {
        if (fragments.Count == 0)
        {
            return;
        }

        Build(output, fragments, lineKind);
    }

    private void Build(
        List<DefaultProjectedPath> output,
        IReadOnlyList<DefaultLineFragment> fragments,
        DefaultLineKind lineKind)
    {
        EnsureScratchCapacity(fragments.Count);
        Array.Clear(_visited, 0, fragments.Count);
        _adjacency.Clear();
        _adjacency.EnsureCapacity(fragments.Count * 2);

        for (var i = 0; i < fragments.Count; i++)
        {
            _startKeys[i] = PointKey(fragments[i].P0);
            _endKeys[i] = PointKey(fragments[i].P1);
            Add(_adjacency, _startKeys[i], new EndpointRef(i, 0));
            Add(_adjacency, _endKeys[i], new EndpointRef(i, 1));
        }

        var pathIndex = 0;

        List<Point2D> Walk(int fragmentIndex, int end)
        {
            var points = new List<Point2D>(8);
            var currentFragment = fragmentIndex;
            var currentEnd = end;
            var guard = 0;

            while (currentFragment >= 0 && !_visited[currentFragment] && guard++ < 20000)
            {
                _visited[currentFragment] = true;
                var fragment = fragments[currentFragment];
                var first = currentEnd == 0 ? fragment.P0 : fragment.P1;
                var second = currentEnd == 0 ? fragment.P1 : fragment.P0;

                if (points.Count == 0)
                {
                    points.Add(first);
                }

                points.Add(second);

                if (!TryGetNextEndpoint(
                    _adjacency,
                    currentEnd == 0 ? _endKeys[currentFragment] : _startKeys[currentFragment],
                    _visited,
                    out var next))
                {
                    break;
                }

                currentFragment = next.FragmentIndex;
                currentEnd = next.End;
            }

            return points;
        }

        foreach (var pair in _adjacency)
        {
            if (pair.Value.Count == 2)
            {
                continue;
            }

            foreach (var endpoint in pair.Value)
            {
                if (_visited[endpoint.FragmentIndex])
                {
                    continue;
                }

                AddPath(output, lineKind, Walk(endpoint.FragmentIndex, endpoint.End), pathIndex++);
            }
        }

        for (var i = 0; i < fragments.Count; i++)
        {
            if (!_visited[i])
            {
                AddPath(output, lineKind, Walk(i, 0), pathIndex++);
            }
        }
    }

    private void EnsureScratchCapacity(int count)
    {
        if (_startKeys.Length < count)
        {
            _startKeys = new EndpointKey[count];
        }

        if (_endKeys.Length < count)
        {
            _endKeys = new EndpointKey[count];
        }

        if (_visited.Length < count)
        {
            _visited = new bool[count];
        }
    }

    private static void AddPath(List<DefaultProjectedPath> paths, DefaultLineKind lineKind, List<Point2D> points, int pathIndex)
    {
        if (points.Count <= 1)
        {
            return;
        }

        var length = DefaultPathMath.PathLength(points);
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

        public readonly Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        public struct Enumerator
        {
            private readonly EndpointBucket _bucket;
            private int _index;

            public Enumerator(EndpointBucket bucket)
            {
                _bucket = bucket;
                _index = -1;
            }

            public readonly EndpointRef Current => _bucket[_index];

            public bool MoveNext()
            {
                _index++;
                return _index < _bucket.Count;
            }
        }
    }

    private static EndpointKey PointKey(Point2D point)
    {
        return new EndpointKey(
            JavaScriptRound(point.X / Quantization),
            JavaScriptRound(point.Y / Quantization));
    }

    private static int JavaScriptRound(float value)
    {
        return (int)MathF.Floor(value + 0.5f);
    }

    private readonly record struct EndpointKey(int X, int Y);

    private readonly record struct EndpointRef(int FragmentIndex, int End);
}
