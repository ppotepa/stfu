using System.Numerics;
using STFU.NPR.Analysis;
using STFU.NPR.Graph;
using STFU.NPR.Pipeline;

namespace STFU.NPR.Steps.Mesh;

public sealed class BuildMeshTopologyStep : INprStep
{
    public void Execute(NprContext context)
    {
        foreach (var projectedMesh in context.Graph.Meshes)
        {
            var cache = context.Analysis.GetOrCreate(projectedMesh.MeshHandle, projectedMesh.Mesh);
            foreach (var cachedEdge in cache.Topology.Edges)
            {
                var firstTriangleIndex = projectedMesh.TriangleOffset + cachedEdge.FirstTriangleIndex;
                var secondTriangleIndex = cachedEdge.SecondTriangleIndex >= 0
                    ? projectedMesh.TriangleOffset + cachedEdge.SecondTriangleIndex
                    : -1;
                var startVertexIndex = projectedMesh.VertexOffset + cachedEdge.StartVertexIndex;
                var endVertexIndex = projectedMesh.VertexOffset + cachedEdge.EndVertexIndex;
                var normalAngle = secondTriangleIndex >= 0
                    ? CalculateNormalAngle(
                        context.Graph.Triangles[firstTriangleIndex].Normal,
                        context.Graph.Triangles[secondTriangleIndex].Normal)
                    : 180f;

                context.Graph.TopologyEdges.Add(new TopologyEdge(
                    cachedEdge.StableId,
                    startVertexIndex,
                    endVertexIndex,
                    firstTriangleIndex,
                    secondTriangleIndex,
                    normalAngle,
                    cachedEdge.IsBoundary));
            }
        }
    }

    private static float CalculateNormalAngle(Vector3 a, Vector3 b)
    {
        var dot = Math.Clamp(Vector3.Dot(a, b), -1f, 1f);
        return MathF.Acos(dot) * 180f / MathF.PI;
    }
}
