using STFU.NPR.Graph;
using STFU.Strokes;

namespace STFU.NPR.Pipeline.Default.Steps;

public sealed class DefaultBuildPathsFromFragmentsStep : STFU.NPR.Pipeline.INprStep
{
    private const float Quantization = 2.5f;

    public void Execute(STFU.NPR.Pipeline.NprContext context)
    {
        context.Graph.DefaultPaths.Clear();

        foreach (var group in context.Graph.DefaultFragments.GroupBy(fragment => fragment.Type))
        {
            var paths = Build(group.ToArray(), group.Key);
            foreach (var path in paths)
            {
                context.Graph.DefaultPaths.Add(path);
            }
        }
    }

    private static IReadOnlyList<DefaultProjectedPath> Build(DefaultLineFragment[] fragments, DefaultLineKind lineKind)
    {
        var adjacency = new Dictionary<string, List<EndpointRef>>(StringComparer.Ordinal);
        var startKeys = new string[fragments.Length];
        var endKeys = new string[fragments.Length];

        for (var i = 0; i < fragments.Length; i++)
        {
            startKeys[i] = PointKey(fragments[i].P0);
            endKeys[i] = PointKey(fragments[i].P1);
            Add(adjacency, startKeys[i], new EndpointRef(i, 0));
            Add(adjacency, endKeys[i], new EndpointRef(i, 1));
        }

        var visited = new HashSet<int>();
        var paths = new List<DefaultProjectedPath>();
        var pathIndex = 0;

        List<Point2D> Walk(int fragmentIndex, int end)
        {
            var points = new List<Point2D>();
            var currentFragment = fragmentIndex;
            var currentEnd = end;
            var guard = 0;

            while (currentFragment >= 0 && !visited.Contains(currentFragment) && guard++ < 20000)
            {
                visited.Add(currentFragment);
                var fragment = fragments[currentFragment];
                var first = currentEnd == 0 ? fragment.P0 : fragment.P1;
                var second = currentEnd == 0 ? fragment.P1 : fragment.P0;

                if (points.Count == 0)
                {
                    points.Add(first);
                }

                points.Add(second);

                var nextKey = currentEnd == 0 ? endKeys[currentFragment] : startKeys[currentFragment];
                var candidates = adjacency.TryGetValue(nextKey, out var refs)
                    ? refs.Where(candidate => !visited.Contains(candidate.FragmentIndex)).ToArray()
                    : [];

                if (candidates.Length == 0)
                {
                    break;
                }

                var next = candidates[0];
                currentFragment = next.FragmentIndex;
                currentEnd = next.End;
            }

            return points;
        }

        foreach (var pair in adjacency)
        {
            var remaining = pair.Value.Where(value => !visited.Contains(value.FragmentIndex)).ToArray();
            if (remaining.Length > 0 && pair.Value.Count != 2)
            {
                foreach (var endpoint in remaining)
                {
                    if (visited.Contains(endpoint.FragmentIndex))
                    {
                        continue;
                    }

                    AddPath(paths, lineKind, Walk(endpoint.FragmentIndex, endpoint.End), pathIndex++);
                }
            }
        }

        for (var i = 0; i < fragments.Length; i++)
        {
            if (!visited.Contains(i))
            {
                AddPath(paths, lineKind, Walk(i, 0), pathIndex++);
            }
        }

        return paths;
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

    private static void Add(Dictionary<string, List<EndpointRef>> adjacency, string key, EndpointRef value)
    {
        if (!adjacency.TryGetValue(key, out var list))
        {
            list = [];
            adjacency[key] = list;
        }

        list.Add(value);
    }

    private static string PointKey(Point2D point)
    {
        return $"{JavaScriptRound(point.X / Quantization)}:{JavaScriptRound(point.Y / Quantization)}";
    }

    private static int JavaScriptRound(float value)
    {
        return (int)MathF.Floor(value + 0.5f);
    }

    private readonly record struct EndpointRef(int FragmentIndex, int End);
}
