using System.Numerics;
using STFU.NPR.Graph;

namespace STFU.NPR.Pipeline.Default.Steps;

public sealed class BuildMeshTopologyStep : STFU.NPR.Pipeline.INprStep
{
    public void Execute(STFU.NPR.Pipeline.NprContext context)
    {
        if (context.Settings.DefaultDrawing.TopologyMode == STFU.NPR.Settings.DefaultTopologyMode.PerTriangleEdges)
        {
            BuildPerTriangleEdges(context);
            return;
        }

        var edges = new Dictionary<long, EdgeInfo>();

        for (var triangleIndex = 0; triangleIndex < context.Graph.Triangles.Count; triangleIndex++)
        {
            var triangle = context.Graph.Triangles[triangleIndex];
            AddEdge(edges, triangle, triangle.A, triangle.B, triangleIndex);
            AddEdge(edges, triangle, triangle.B, triangle.C, triangleIndex);
            AddEdge(edges, triangle, triangle.C, triangle.A, triangleIndex);
        }

        foreach (var entry in edges.Values)
        {
            var firstTriangle = context.Graph.Triangles[entry.FirstTriangleIndex];
            var secondTriangle = entry.SecondTriangleIndex >= 0
                ? context.Graph.Triangles[entry.SecondTriangleIndex]
                : default;

            var angleDegrees = entry.SecondTriangleIndex >= 0
                ? AngleDegrees(firstTriangle.Normal, secondTriangle.Normal)
                : 180f;

            var a = context.Graph.Vertices[entry.StartVertexIndex];
            var b = context.Graph.Vertices[entry.EndVertexIndex];
            var depth = (a.Depth + b.Depth) * 0.5f;

            context.Graph.TopologyEdges.Add(new TopologyEdge(
                entry.StableId,
                entry.StartVertexIndex,
                entry.EndVertexIndex,
                entry.FirstTriangleIndex,
                entry.SecondTriangleIndex,
                angleDegrees,
                entry.SecondTriangleIndex < 0)
            {
                EntityId = firstTriangle.EntityId
            });

            context.Graph.Edges.Add(new ProjectedEdge(entry.StableId, entry.StartVertexIndex, entry.EndVertexIndex, depth));
        }
    }

    private static void BuildPerTriangleEdges(STFU.NPR.Pipeline.NprContext context)
    {
        var edgeCapacity = context.Graph.Triangles.Count * 3;
        context.Graph.TopologyEdges.EnsureCapacity(context.Graph.TopologyEdges.Count + edgeCapacity);
        context.Graph.Edges.EnsureCapacity(context.Graph.Edges.Count + edgeCapacity);

        for (var triangleIndex = 0; triangleIndex < context.Graph.Triangles.Count; triangleIndex++)
        {
            var triangle = context.Graph.Triangles[triangleIndex];
            AddPerTriangleEdge(context, triangle, triangle.A, triangle.B, triangleIndex, 0);
            AddPerTriangleEdge(context, triangle, triangle.B, triangle.C, triangleIndex, 1);
            AddPerTriangleEdge(context, triangle, triangle.C, triangle.A, triangleIndex, 2);
        }
    }

    private static void AddPerTriangleEdge(
        STFU.NPR.Pipeline.NprContext context,
        ProjectedTriangle triangle,
        int aIndex,
        int bIndex,
        int triangleIndex,
        int edgeIndex)
    {
        if ((uint)aIndex >= (uint)context.Graph.Vertices.Count ||
            (uint)bIndex >= (uint)context.Graph.Vertices.Count)
        {
            return;
        }

        var a = context.Graph.Vertices[aIndex];
        var b = context.Graph.Vertices[bIndex];
        var depth = (a.Depth + b.Depth) * 0.5f;
        var stableId = (triangle.StableId * 397) ^ (edgeIndex * 131) ^ aIndex ^ (bIndex * 17);

        context.Graph.TopologyEdges.Add(new TopologyEdge(
            stableId,
            aIndex,
            bIndex,
            triangleIndex,
            -1,
            180f,
            true)
        {
            EntityId = triangle.EntityId
        });

        context.Graph.Edges.Add(new ProjectedEdge(stableId, aIndex, bIndex, depth));
    }

    private static void AddEdge(Dictionary<long, EdgeInfo> edges, ProjectedTriangle triangle, int a, int b, int triangleIndex)
    {
        var min = Math.Min(a, b);
        var max = Math.Max(a, b);
        var key = ((long)min << 32) | (uint)max;

        if (!edges.TryGetValue(key, out var info))
        {
            info = new EdgeInfo((triangle.StableId * 397) ^ (a * 17) ^ b, min, max, triangleIndex, -1);
        }
        else if (info.SecondTriangleIndex < 0)
        {
            info = info with { SecondTriangleIndex = triangleIndex };
        }

        edges[key] = info;
    }

    private static float AngleDegrees(Vector3 a, Vector3 b)
    {
        var dot = Math.Clamp(Vector3.Dot(Vector3.Normalize(a), Vector3.Normalize(b)), -1f, 1f);
        return MathF.Acos(dot) * 180f / MathF.PI;
    }

    private readonly record struct EdgeInfo(
        int StableId,
        int StartVertexIndex,
        int EndVertexIndex,
        int FirstTriangleIndex,
        int SecondTriangleIndex);
}
