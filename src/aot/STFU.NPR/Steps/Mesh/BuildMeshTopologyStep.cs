using System.Numerics;
using STFU.NPR.Graph;
using STFU.NPR.Pipeline;

namespace STFU.NPR.Steps.Mesh;

public sealed class BuildMeshTopologyStep : INprStep
{
    public void Execute(NprContext context)
    {
        var edges = new Dictionary<long, PendingEdge>();

        for (var triangleIndex = 0; triangleIndex < context.Graph.Triangles.Count; triangleIndex++)
        {
            var triangle = context.Graph.Triangles[triangleIndex];
            AddEdge(edges, triangle.A, triangle.B, triangleIndex);
            AddEdge(edges, triangle.B, triangle.C, triangleIndex);
            AddEdge(edges, triangle.C, triangle.A, triangleIndex);
        }

        foreach (var edge in edges.Values)
        {
            var normalAngle = edge.SecondTriangleIndex >= 0
                ? CalculateNormalAngle(
                    context.Graph.Triangles[edge.FirstTriangleIndex].Normal,
                    context.Graph.Triangles[edge.SecondTriangleIndex].Normal)
                : 180f;

            context.Graph.TopologyEdges.Add(new TopologyEdge(
                StableEdgeId(edge.StartVertexIndex, edge.EndVertexIndex),
                edge.StartVertexIndex,
                edge.EndVertexIndex,
                edge.FirstTriangleIndex,
                edge.SecondTriangleIndex,
                normalAngle,
                edge.SecondTriangleIndex < 0));
        }
    }

    private static void AddEdge(Dictionary<long, PendingEdge> edges, int a, int b, int triangleIndex)
    {
        var key = CreateEdgeKey(a, b);

        if (edges.TryGetValue(key, out var edge))
        {
            if (edge.SecondTriangleIndex < 0)
            {
                edges[key] = edge with { SecondTriangleIndex = triangleIndex };
            }

            return;
        }

        edges[key] = new PendingEdge(Math.Min(a, b), Math.Max(a, b), triangleIndex, -1);
    }

    private static float CalculateNormalAngle(Vector3 a, Vector3 b)
    {
        var dot = Math.Clamp(Vector3.Dot(a, b), -1f, 1f);
        return MathF.Acos(dot) * 180f / MathF.PI;
    }

    private static long CreateEdgeKey(int a, int b)
    {
        var min = Math.Min(a, b);
        var max = Math.Max(a, b);
        return ((long)min << 32) | (uint)max;
    }

    private static int StableEdgeId(int a, int b)
    {
        unchecked
        {
            return Math.Min(a, b) * 83492791 ^ Math.Max(a, b) * 297121507;
        }
    }

    private readonly record struct PendingEdge(
        int StartVertexIndex,
        int EndVertexIndex,
        int FirstTriangleIndex,
        int SecondTriangleIndex);
}
