using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;

namespace STFU.NPR.Pipeline.InteractivePerformance.Stages;

public sealed class CandidateEdgeStage : IInteractivePipelineStage
{
    public string Name => "InteractiveCandidateEdges";

    public bool ShouldRun(InteractiveFrameContext context)
    {
        return context.WorkClass is
            InteractiveWorkClass.StrokeCandidateRefresh or
            InteractiveWorkClass.FullVisibleStrokeRefresh;
    }

    public void Execute(InteractiveFrameContext context)
    {
        var visibleFaces = LoadVisibleFaceSet(context);
        var source = ResolveSource(context);
        var key = ArtifactKeyFactory.CandidateEdges(context.Intent, source.TotalEdgeCount);

        if (context.Artifacts.TryGet<CandidateEdgeArtifact>(key, out var cached))
        {
            context.Diagnostics.CacheHits++;
            WriteDiagnostics(context, cached, source);
            return;
        }

        var edges = BuildCandidateEdges(context, visibleFaces, source);
        var artifact = new CandidateEdgeArtifact
        {
            Key = key,
            Revision = context.Intent.FrameId,
            LastBuildTime = TimeSpan.Zero,
            TotalEdgeCount = Math.Max(source.TotalEdgeCount, edges.Length),
            Edges = edges
        };

        context.Artifacts.Set(artifact);
        context.Diagnostics.CacheMisses++;
        WriteDiagnostics(context, artifact, source);
    }

    private static CandidateEdgeSourceInfo ResolveSource(InteractiveFrameContext context)
    {
        var graph = context.ReferenceContext.Graph;
        if (graph.DefaultFragments.Count > 0)
        {
            return new CandidateEdgeSourceInfo(
                InteractiveCandidateEdgeSource.ReferenceFragments,
                graph.DefaultFragments.Count,
                ReferenceFragmentCount: graph.DefaultFragments.Count,
                ProjectedTriangleCount: 0);
        }

        if (context.Artifacts.TryGetLatest(ArtifactKind.ProjectedTriangles, out ProjectedTriangleArtifact projectedTriangles) &&
            context.Artifacts.TryGetLatest(ArtifactKind.ProjectedVertices, out ProjectedVertexArtifact projectedVertices) &&
            projectedTriangles.TriangleCount > 0 &&
            projectedVertices.VertexCount > 0)
        {
            return new CandidateEdgeSourceInfo(
                InteractiveCandidateEdgeSource.ProjectedTriangleEdges,
                ProjectedTriangleCandidateEdgeBuilder.EstimateTotalEdgeCount(projectedTriangles),
                ReferenceFragmentCount: 0,
                ProjectedTriangleCount: projectedTriangles.TriangleCount);
        }

        return new CandidateEdgeSourceInfo(
            InteractiveCandidateEdgeSource.None,
            graph.TopologyEdges.Count,
            ReferenceFragmentCount: 0,
            ProjectedTriangleCount: 0);
    }

    private static HashSet<int>? LoadVisibleFaceSet(InteractiveFrameContext context)
    {
        return context.Artifacts.TryGetLatest(ArtifactKind.VisibleFaces, out VisibleFaceSetArtifact visibleFaces)
            ? visibleFaces.VisibleFaceIndices.ToHashSet()
            : null;
    }

    private static InteractiveCandidateEdge[] BuildCandidateEdges(
        InteractiveFrameContext context,
        IReadOnlySet<int>? visibleFaces,
        CandidateEdgeSourceInfo source)
    {
        return source.Source switch
        {
            InteractiveCandidateEdgeSource.ReferenceFragments => BuildFromReferenceFragments(context, visibleFaces),
            InteractiveCandidateEdgeSource.ProjectedTriangleEdges => BuildFromProjectedTriangles(context, visibleFaces),
            _ => []
        };
    }

    private static InteractiveCandidateEdge[] BuildFromReferenceFragments(
        InteractiveFrameContext context,
        IReadOnlySet<int>? visibleFaces)
    {
        var fragments = context.ReferenceContext.Graph.DefaultFragments;
        if (fragments.Count == 0)
        {
            return [];
        }

        var candidates = new List<InteractiveCandidateEdge>(fragments.Count);
        for (var index = 0; index < fragments.Count; index++)
        {
            var fragment = fragments[index];
            if (visibleFaces is not null &&
                !visibleFaces.Contains(fragment.FirstTriangleIndex) &&
                !visibleFaces.Contains(fragment.SecondTriangleIndex))
            {
                continue;
            }

            candidates.Add(new InteractiveCandidateEdge(
                SourceEdgeId: fragment.EdgeStableId,
                FaceA: fragment.FirstTriangleIndex,
                FaceB: fragment.SecondTriangleIndex,
                Role: (int)fragment.Type,
                X0: fragment.P0.X,
                Y0: fragment.P0.Y,
                X1: fragment.P1.X,
                Y1: fragment.P1.Y,
                ProjectedLength: Distance(fragment.P0.X, fragment.P0.Y, fragment.P1.X, fragment.P1.Y),
                Depth: fragment.Depth,
                Importance: 1f));
        }

        return candidates.ToArray();
    }

    private static InteractiveCandidateEdge[] BuildFromProjectedTriangles(
        InteractiveFrameContext context,
        IReadOnlySet<int>? visibleFaces)
    {
        if (!context.Artifacts.TryGetLatest(ArtifactKind.ProjectedTriangles, out ProjectedTriangleArtifact projectedTriangles) ||
            !context.Artifacts.TryGetLatest(ArtifactKind.ProjectedVertices, out ProjectedVertexArtifact projectedVertices))
        {
            return [];
        }

        return ProjectedTriangleCandidateEdgeBuilder.BuildEdges(
            projectedTriangles.Triangles,
            projectedVertices.Vertices,
            visibleFaces);
    }

    private static void WriteDiagnostics(
        InteractiveFrameContext context,
        CandidateEdgeArtifact artifact,
        CandidateEdgeSourceInfo source)
    {
        context.Diagnostics.TotalEdges = artifact.TotalEdgeCount;
        context.Diagnostics.CandidateEdges = artifact.CandidateEdgeCount;
        context.Diagnostics.CandidateReductionPercent = artifact.CandidateReductionPercent;
        context.Diagnostics.CandidateEdgeSource = (long)source.Source;
        context.Diagnostics.CandidateEdgeSourceReferenceFragments = source.ReferenceFragmentCount;
        context.Diagnostics.CandidateEdgeSourceProjectedTriangles = source.ProjectedTriangleCount;
    }

    private static float Distance(float x0, float y0, float x1, float y1)
    {
        var dx = x1 - x0;
        var dy = y1 - y0;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    private readonly record struct CandidateEdgeSourceInfo(
        InteractiveCandidateEdgeSource Source,
        int TotalEdgeCount,
        int ReferenceFragmentCount,
        int ProjectedTriangleCount);
}
