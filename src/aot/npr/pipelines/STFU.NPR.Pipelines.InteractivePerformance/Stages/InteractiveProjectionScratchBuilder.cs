using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.NPR.Graph;

namespace STFU.NPR.Pipeline.InteractivePerformance.Stages;

internal static class InteractiveProjectionScratchBuilder
{
    public static InteractiveProjectionSnapshot Build(InteractiveFrameContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var input = InteractiveProjectionInputBuilder.Build(context.ReferenceContext);
        if (!input.HasGeometry)
        {
            return new InteractiveProjectionSnapshot(
                new NprGraph(),
                InteractiveProjectionSource.ScratchProjection,
                context.ReferenceContext.Scene.Entities.Count,
                InputMeshCount: input.MeshCount,
                InputVertexCount: input.VertexCount,
                InputTriangleCount: input.TriangleCount,
                ProjectedMeshCount: 0,
                ProjectedVertexCount: 0,
                ProjectedTriangleCount: 0,
                Note: input.SourceNote,
                UsedReferenceGraph: false);
        }

        var triangleGraph = InteractiveProjectionGraphBuilder.Build(input);

        return new InteractiveProjectionSnapshot(
            triangleGraph,
            InteractiveProjectionSource.ScratchProjection,
            context.ReferenceContext.Scene.Entities.Count,
            InputMeshCount: input.MeshCount,
            InputVertexCount: input.VertexCount,
            InputTriangleCount: input.TriangleCount,
            ProjectedMeshCount: triangleGraph.Meshes.Count,
            ProjectedVertexCount: triangleGraph.Vertices.Count,
            ProjectedTriangleCount: triangleGraph.Triangles.Count,
            Note: "Scratch projection built from scene/assets input without ReferenceGraph.",
            UsedReferenceGraph: false);
    }
}
