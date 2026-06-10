using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;

namespace STFU.NPR.Pipeline.InteractivePerformance.Stages;

public static class ProjectedTriangleCandidateEdgeBuilder
{
    public static InteractiveCandidateEdge[] BuildEdges(
        IReadOnlyList<InteractiveProjectedTriangle> triangles,
        IReadOnlyList<InteractiveProjectedVertex> vertices,
        IReadOnlySet<int>? visibleFaces)
    {
        if (triangles.Count == 0 || vertices.Count == 0)
        {
            return [];
        }

        var edges = new Dictionary<long, EdgeSeed>(triangles.Count * 3);
        for (var triangleIndex = 0; triangleIndex < triangles.Count; triangleIndex++)
        {
            var triangle = triangles[triangleIndex];
            if (!ShouldUseTriangle(triangle, visibleFaces))
            {
                continue;
            }

            TryAddEdge(edges, triangle, triangle.A, triangle.B);
            TryAddEdge(edges, triangle, triangle.B, triangle.C);
            TryAddEdge(edges, triangle, triangle.C, triangle.A);
        }

        if (edges.Count == 0)
        {
            return [];
        }

        var candidates = new List<InteractiveCandidateEdge>(edges.Count);
        foreach (var edge in edges.Values)
        {
            if (!TryResolveVertex(vertices, edge.A, out var a) ||
                !TryResolveVertex(vertices, edge.B, out var b))
            {
                continue;
            }

            var length = Distance(a.X, a.Y, b.X, b.Y);
            if (length <= 0.25f)
            {
                continue;
            }

            candidates.Add(new InteractiveCandidateEdge(
                SourceEdgeId: edge.StableId,
                FaceA: edge.FirstFace,
                FaceB: edge.SecondFace,
                Role: ResolveRole(edge),
                X0: a.X,
                Y0: a.Y,
                X1: b.X,
                Y1: b.Y,
                ProjectedLength: length,
                Depth: edge.Depth,
                Importance: ResolveImportance(edge, length)));
        }

        return candidates.ToArray();
    }

    public static int EstimateTotalEdgeCount(ProjectedTriangleArtifact? triangles)
    {
        return triangles is null ? 0 : triangles.TriangleCount * 3;
    }

    private static bool ShouldUseTriangle(
        InteractiveProjectedTriangle triangle,
        IReadOnlySet<int>? visibleFaces)
    {
        if (!triangle.IsVisible)
        {
            return false;
        }

        if (visibleFaces is not null && !visibleFaces.Contains(triangle.SourceIndex))
        {
            return false;
        }

        return true;
    }

    private static void TryAddEdge(
        Dictionary<long, EdgeSeed> edges,
        InteractiveProjectedTriangle triangle,
        int a,
        int b)
    {
        if (a < 0 || b < 0 || a == b)
        {
            return;
        }

        var min = Math.Min(a, b);
        var max = Math.Max(a, b);
        var key = EdgeKey(min, max);
        if (edges.TryGetValue(key, out var existing))
        {
            edges[key] = existing with
            {
                SecondFace = triangle.SourceIndex,
                SharedCount = existing.SharedCount + 1,
                Depth = Math.Min(existing.Depth, triangle.Depth),
                Shade = (existing.Shade + triangle.Shade) * 0.5f
            };
            return;
        }

        edges[key] = new EdgeSeed(
            StableId: triangle.StableId != 0 ? triangle.StableId * 3 + EdgeSlot(a, b, triangle) : triangle.SourceIndex * 3 + EdgeSlot(a, b, triangle),
            A: min,
            B: max,
            FirstFace: triangle.SourceIndex,
            SecondFace: -1,
            SharedCount: 1,
            Depth: triangle.Depth,
            Shade: triangle.Shade,
            FrontFacing: triangle.IsFrontFacing);
    }

    private static int EdgeSlot(int a, int b, InteractiveProjectedTriangle triangle)
    {
        if ((a == triangle.A && b == triangle.B) || (a == triangle.B && b == triangle.A))
        {
            return 0;
        }

        if ((a == triangle.B && b == triangle.C) || (a == triangle.C && b == triangle.B))
        {
            return 1;
        }

        return 2;
    }

    private static bool TryResolveVertex(
        IReadOnlyList<InteractiveProjectedVertex> vertices,
        int index,
        out InteractiveProjectedVertex vertex)
    {
        if ((uint)index < (uint)vertices.Count)
        {
            vertex = vertices[index];
            return vertex.IsVisible;
        }

        vertex = default;
        return false;
    }

    private static int ResolveRole(EdgeSeed edge)
    {
        if (edge.SharedCount <= 1 || edge.SecondFace < 0)
        {
            return 0;
        }

        return edge.FrontFacing ? 2 : 1;
    }

    private static float ResolveImportance(EdgeSeed edge, float length)
    {
        var roleBoost = edge.SharedCount <= 1 ? 1.25f : 0.85f;
        var lengthBoost = Math.Clamp(length / 96f, 0.35f, 1.35f);
        var shadeBoost = Math.Clamp(1f - edge.Shade * 0.25f, 0.75f, 1.2f);
        return Math.Clamp(roleBoost * lengthBoost * shadeBoost, 0.25f, 2.0f);
    }

    private static float Distance(float x0, float y0, float x1, float y1)
    {
        var dx = x1 - x0;
        var dy = y1 - y0;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private static long EdgeKey(int a, int b)
    {
        return ((long)a << 32) ^ (uint)b;
    }

    private readonly record struct EdgeSeed(
        int StableId,
        int A,
        int B,
        int FirstFace,
        int SecondFace,
        int SharedCount,
        float Depth,
        float Shade,
        bool FrontFacing);
}
