using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;

namespace STFU.NPR.Pipeline.InteractivePerformance.Stages;

public sealed class ProjectionStage : IInteractivePipelineStage
{
    public string Name => "InteractiveProjection";

    public bool ShouldRun(InteractiveFrameContext context)
    {
        return context.WorkClass is
            InteractiveWorkClass.ProjectionOnly or
            InteractiveWorkClass.VisibilityRefresh or
            InteractiveWorkClass.StrokeCandidateRefresh or
            InteractiveWorkClass.FullVisibleStrokeRefresh;
    }

    public void Execute(InteractiveFrameContext context)
    {
        var graph = context.ReferenceContext.Graph;
        var summaryKey = ArtifactKeyFactory.ProjectionSummary(context.Intent);
        var verticesKey = ArtifactKeyFactory.ProjectedVertices(context.Intent, graph.Vertices.Count);
        var trianglesKey = ArtifactKeyFactory.ProjectedTriangles(context.Intent, graph.Triangles.Count);

        var hadSummary = context.Artifacts.TryGet<ProjectionSummaryArtifact>(summaryKey, out var summary);
        var hadVertices = context.Artifacts.TryGet<ProjectedVertexArtifact>(verticesKey, out var vertices);
        var hadTriangles = context.Artifacts.TryGet<ProjectedTriangleArtifact>(trianglesKey, out var triangles);

        if (hadSummary && hadVertices && hadTriangles)
        {
            context.Diagnostics.CacheHits++;
            WriteDiagnostics(context, vertices, triangles);
            return;
        }

        if (!hadVertices)
        {
            vertices = ProjectionArtifactBuilder.BuildVertices(context, verticesKey);
            context.Artifacts.Set(vertices);
            context.Diagnostics.CacheMisses++;
        }
        else
        {
            context.Diagnostics.CacheHits++;
        }

        if (!hadTriangles)
        {
            triangles = ProjectionArtifactBuilder.BuildTriangles(context, trianglesKey);
            context.Artifacts.Set(triangles);
            context.Diagnostics.CacheMisses++;
        }
        else
        {
            context.Diagnostics.CacheHits++;
        }

        if (!hadSummary)
        {
            summary = BuildSummary(context, summaryKey, vertices, triangles);
            context.Artifacts.Set(summary);
            context.Diagnostics.CacheMisses++;
        }
        else
        {
            context.Diagnostics.CacheHits++;
        }

        WriteDiagnostics(context, vertices, triangles);
    }

    private static ProjectionSummaryArtifact BuildSummary(
        InteractiveFrameContext context,
        ArtifactKey key,
        ProjectedVertexArtifact vertices,
        ProjectedTriangleArtifact triangles)
    {
        var fullProjectionAvailable = vertices.VertexCount > 0 || triangles.TriangleCount > 0;
        var note = fullProjectionAvailable
            ? "Interactive projection artifacts harvested from the Reference Quality graph."
            : "Interactive projection artifacts are empty because the Reference Quality graph had no projected geometry.";

        return new ProjectionSummaryArtifact
        {
            Key = key,
            Revision = context.Intent.FrameId,
            Width = context.Intent.Width,
            Height = context.Intent.Height,
            FullProjectionAvailable = fullProjectionAvailable,
            LastBuildTime = TimeSpan.Zero,
            Note = note
        };
    }

    private static void WriteDiagnostics(
        InteractiveFrameContext context,
        ProjectedVertexArtifact vertices,
        ProjectedTriangleArtifact triangles)
    {
        context.Diagnostics.ProjectedVertices = vertices.VertexCount;
        context.Diagnostics.ProjectedTriangles = triangles.TriangleCount;
        context.Diagnostics.VisibleProjectedVertices = vertices.VisibleVertexCount;
        context.Diagnostics.VisibleProjectedTriangles = triangles.VisibleTriangleCount;
        context.Diagnostics.FrontFacingProjectedTriangles = triangles.FrontFacingTriangleCount;
    }
}
