using STFU.NPR.Graph;
using STFU.Strokes;

namespace STFU.NPR.Visibility;

internal sealed class ScreenSpaceBvh
{
    private const int LeafTriangleCount = 8;

    private readonly ProjectedTriangle[] _triangles;
    private readonly ScreenSpaceBounds[] _bounds;

    private ScreenSpaceBvh(ProjectedTriangle[] triangles, ScreenSpaceBounds[] bounds, ScreenSpaceBvhNode? root)
    {
        _triangles = triangles;
        _bounds = bounds;
        Root = root;
    }

    public ScreenSpaceBvhNode? Root { get; }

    public int TriangleCount => _triangles.Length;

    public static ScreenSpaceBvh Build(IReadOnlyList<ProjectedTriangle> triangles, IReadOnlyList<ProjectedVertex> vertices)
    {
        if (triangles.Count == 0)
        {
            return new ScreenSpaceBvh([], [], null);
        }

        var ownedTriangles = triangles.ToArray();
        var bounds = new ScreenSpaceBounds[ownedTriangles.Length];
        for (var index = 0; index < ownedTriangles.Length; index++)
        {
            bounds[index] = ComputeBounds(ownedTriangles[index], vertices);
        }

        var root = BuildNode(ownedTriangles, bounds, 0, ownedTriangles.Length);
        return new ScreenSpaceBvh(ownedTriangles, bounds, root);
    }

    public bool IsOccluded(Point2D point, float lineDepth, float depthBias, IReadOnlyList<ProjectedVertex> vertices)
    {
        return Root is not null && IsOccluded(Root, point, lineDepth, depthBias, vertices);
    }

    private bool IsOccluded(
        ScreenSpaceBvhNode node,
        Point2D point,
        float lineDepth,
        float depthBias,
        IReadOnlyList<ProjectedVertex> vertices)
    {
        if (!node.Bounds.Contains(point.X, point.Y))
        {
            return false;
        }

        if (node.IsLeaf)
        {
            for (var index = node.Start; index < node.Start + node.Count; index++)
            {
                var triangle = _triangles[index];
                if (!triangle.IsVisible ||
                    !triangle.IsFrontFacing ||
                    triangle.Depth >= lineDepth - depthBias)
                {
                    continue;
                }

                var a = vertices[triangle.A].Position;
                var b = vertices[triangle.B].Position;
                var c = vertices[triangle.C].Position;
                if (ContainsPoint(point, a, b, c))
                {
                    return true;
                }
            }

            return false;
        }

        return (node.Left is not null && IsOccluded(node.Left, point, lineDepth, depthBias, vertices)) ||
            (node.Right is not null && IsOccluded(node.Right, point, lineDepth, depthBias, vertices));
    }

    private static ScreenSpaceBvhNode BuildNode(
        ProjectedTriangle[] triangles,
        ScreenSpaceBounds[] bounds,
        int start,
        int count)
    {
        var aggregate = bounds[start];
        for (var index = start + 1; index < start + count; index++)
        {
            aggregate = ScreenSpaceBounds.Union(aggregate, bounds[index]);
        }

        if (count <= LeafTriangleCount)
        {
            return new ScreenSpaceBvhNode(aggregate, start, count, null, null);
        }

        var splitOnX = aggregate.Width >= aggregate.Height;
        Array.Sort(bounds, triangles, start, count, Comparer<ScreenSpaceBounds>.Create((left, right) =>
        {
            var leftCenter = splitOnX
                ? (left.MinX + left.MaxX) * 0.5f
                : (left.MinY + left.MaxY) * 0.5f;
            var rightCenter = splitOnX
                ? (right.MinX + right.MaxX) * 0.5f
                : (right.MinY + right.MaxY) * 0.5f;
            return leftCenter.CompareTo(rightCenter);
        }));

        var mid = start + count / 2;
        var leftNode = BuildNode(triangles, bounds, start, mid - start);
        var rightNode = BuildNode(triangles, bounds, mid, start + count - mid);
        return new ScreenSpaceBvhNode(aggregate, start, count, leftNode, rightNode);
    }

    private static ScreenSpaceBounds ComputeBounds(ProjectedTriangle triangle, IReadOnlyList<ProjectedVertex> vertices)
    {
        var a = vertices[triangle.A].Position;
        var b = vertices[triangle.B].Position;
        var c = vertices[triangle.C].Position;
        return new ScreenSpaceBounds(
            MathF.Min(a.X, MathF.Min(b.X, c.X)),
            MathF.Min(a.Y, MathF.Min(b.Y, c.Y)),
            MathF.Max(a.X, MathF.Max(b.X, c.X)),
            MathF.Max(a.Y, MathF.Max(b.Y, c.Y)));
    }

    private static bool ContainsPoint(Point2D p, Point2D a, Point2D b, Point2D c)
    {
        var d1 = Sign(p, a, b);
        var d2 = Sign(p, b, c);
        var d3 = Sign(p, c, a);
        var hasNegative = d1 < 0 || d2 < 0 || d3 < 0;
        var hasPositive = d1 > 0 || d2 > 0 || d3 > 0;
        return !(hasNegative && hasPositive);
    }

    private static float Sign(Point2D p1, Point2D p2, Point2D p3)
    {
        return (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
    }
}
