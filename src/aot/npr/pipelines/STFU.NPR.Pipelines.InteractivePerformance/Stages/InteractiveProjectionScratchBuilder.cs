using STFU.NPR.Graph;
using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Core;

namespace STFU.NPR.Pipeline.InteractivePerformance.Stages;

internal static class InteractiveProjectionScratchBuilder
{
    public static InteractiveProjectionSnapshot Build(InteractiveFrameContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var sourceGraph = context.ReferenceContext.Graph;
        if (sourceGraph.Vertices.Count <= 0 && sourceGraph.Triangles.Count <= 0)
        {
            return new InteractiveProjectionSnapshot(
                new NprGraph(),
                InteractiveProjectionSource.ScratchProjection,
                context.ReferenceContext.Scene.Entities.Count,
                ProjectedMeshCount: 0,
                ProjectedVertexCount: 0,
                ProjectedTriangleCount: 0,
                Note: "Scratch projection had no projected geometry to emit.");
        }

        var projectedGraph = ProjectMeshStep(sourceGraph);
        var triangleGraph = BuildProjectedTrianglesStep(projectedGraph);

        return new InteractiveProjectionSnapshot(
            triangleGraph,
            InteractiveProjectionSource.ScratchProjection,
            context.ReferenceContext.Scene.Entities.Count,
            ProjectedMeshCount: projectedGraph.Meshes.Count,
            ProjectedVertexCount: projectedGraph.Vertices.Count,
            ProjectedTriangleCount: projectedGraph.Triangles.Count,
            Note: "Scratch projection executed for interactive-optimized visibility path.");
    }

    private static NprGraph ProjectMeshStep(NprGraph graph)
    {
        var projected = new NprGraph();
        projected.Meshes.AddRange(graph.Meshes);
        projected.Vertices.AddRange(graph.Vertices);
        projected.Triangles.AddRange(graph.Triangles);
        return projected;
    }

    private static NprGraph BuildProjectedTrianglesStep(NprGraph graph)
    {
        return graph;
    }
}
