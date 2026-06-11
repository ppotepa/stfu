using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;
using STFU.NPR.Pipelines.Abstractions;

namespace STFU.NPR.Pipeline.InteractivePerformance.Stages;

public sealed class ProjectionStage : IInteractivePipelineStage
{
    private readonly FramePipelineStrategyOptions _options;

    public ProjectionStage()
        : this(FramePipelineStrategyOptions.Default)
    {
    }

    public ProjectionStage(FramePipelineStrategyOptions options)
    {
        _options = options ?? FramePipelineStrategyOptions.Default;
    }

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
        var referenceGraph = context.ReferenceContext.Graph;
        var projectedVertexCount = ResolveProjectedVertexKeyCount(context, referenceGraph.Vertices.Count);
        var projectedTriangleCount = ResolveProjectedTriangleKeyCount(context, referenceGraph.Triangles.Count);

        var summaryKey = ArtifactKeyFactory.ProjectionSummary(context.Intent);
        var verticesKey = ArtifactKeyFactory.ProjectedVertices(context.Intent, projectedVertexCount);
        var trianglesKey = ArtifactKeyFactory.ProjectedTriangles(context.Intent, projectedTriangleCount);

        var hadSummary = context.Artifacts.TryGet<ProjectionSummaryArtifact>(summaryKey, out var summary);
        var hadVertices = context.Artifacts.TryGet<ProjectedVertexArtifact>(verticesKey, out var vertices);
        var hadTriangles = context.Artifacts.TryGet<ProjectedTriangleArtifact>(trianglesKey, out var triangles);

        if (hadSummary && hadVertices && hadTriangles)
        {
            context.Diagnostics.CacheHits++;
            context.Diagnostics.ProjectionSource = (long)summary.Source;
            WriteDiagnostics(context, summary, vertices, triangles);
            return;
        }

        var built = ProjectionArtifactBuilder.BuildAll(context, summaryKey, verticesKey, trianglesKey, _options);
        context.Artifacts.Set(built.Vertices);
        context.Artifacts.Set(built.Triangles);
        context.Artifacts.Set(built.Summary);

        context.Diagnostics.CacheMisses += CountMisses(hadSummary, hadVertices, hadTriangles);
        WriteDiagnostics(context, built.Summary, built.Vertices, built.Triangles);
    }

    private static int ResolveProjectedVertexKeyCount(InteractiveFrameContext context, int referenceCount)
    {
        if (referenceCount > 0)
        {
            return referenceCount;
        }

        return context.ReferenceContext.Scene.Entities.Count;
    }

    private static int ResolveProjectedTriangleKeyCount(InteractiveFrameContext context, int referenceCount)
    {
        if (referenceCount > 0)
        {
            return referenceCount;
        }

        return context.ReferenceContext.Scene.Entities.Count;
    }

    private static int CountMisses(params bool[] hits)
    {
        var misses = 0;
        foreach (var hit in hits)
        {
            if (!hit)
            {
                misses++;
            }
        }

        return misses;
    }

    private static void WriteDiagnostics(
        InteractiveFrameContext context,
        ProjectionSummaryArtifact summary,
        ProjectedVertexArtifact vertices,
        ProjectedTriangleArtifact triangles)
    {
        context.Diagnostics.ProjectedVertices = vertices.VertexCount;
        context.Diagnostics.ProjectedTriangles = triangles.TriangleCount;
        context.Diagnostics.VisibleProjectedVertices = vertices.VisibleVertexCount;
        context.Diagnostics.VisibleProjectedTriangles = triangles.VisibleTriangleCount;
        context.Diagnostics.FrontFacingProjectedTriangles = triangles.FrontFacingTriangleCount;
        context.Diagnostics.ProjectionSource = (long)summary.Source;
        context.Diagnostics.ProjectionSourceEntities = summary.SourceEntityCount;
        context.Diagnostics.ProjectionMeshes = summary.ProjectedMeshCount;
        context.Diagnostics.ProjectionBuiltSelfContained = summary.IsSelfContained;
        context.Diagnostics.ProjectionUsedReferenceGraph = summary.UsedReferenceGraph;
        context.Diagnostics.ProjectionInputMeshCount = summary.InputMeshCount;
        context.Diagnostics.ProjectionInputVertexCount = summary.InputVertexCount;
        context.Diagnostics.ProjectionInputTriangleCount = summary.InputTriangleCount;
        context.Diagnostics.ProjectionInputSourceNote = summary.Note;
    }
}
