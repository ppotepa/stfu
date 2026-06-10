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
        var graph = context.ReferenceContext.Graph;
        var totalEdges = graph.DefaultFragments.Count > 0
            ? graph.DefaultFragments.Count
            : graph.TopologyEdges.Count;
        var key = ArtifactKeyFactory.CandidateEdges(context.Intent, totalEdges);

        if (context.Artifacts.TryGet<CandidateEdgeArtifact>(key, out var cached))
        {
            context.Diagnostics.CacheHits++;
            context.Diagnostics.TotalEdges = cached.TotalEdgeCount;
            context.Diagnostics.CandidateEdges = cached.CandidateEdgeCount;
            context.Diagnostics.CandidateReductionPercent = cached.CandidateReductionPercent;
            return;
        }

        var visibleFaces = LoadVisibleFaceSet(context);
        var edges = BuildCandidateEdges(context, visibleFaces);
        var artifact = new CandidateEdgeArtifact
        {
            Key = key,
            Revision = context.Intent.FrameId,
            LastBuildTime = TimeSpan.Zero,
            TotalEdgeCount = totalEdges,
            Edges = edges
        };

        context.Artifacts.Set(artifact);
        context.Diagnostics.CacheMisses++;
        context.Diagnostics.TotalEdges = artifact.TotalEdgeCount;
        context.Diagnostics.CandidateEdges = artifact.CandidateEdgeCount;
        context.Diagnostics.CandidateReductionPercent = artifact.CandidateReductionPercent;
    }

    private static HashSet<int> LoadVisibleFaceSet(InteractiveFrameContext context)
    {
        var key = ArtifactKeyFactory.VisibleFaces(context.Intent, context.ReferenceContext.Graph.Triangles.Count);

        return context.Artifacts.TryGet<VisibleFaceSetArtifact>(key, out var visibleFaces)
            ? visibleFaces.VisibleFaceIndices.ToHashSet()
            : [];
    }

    private static InteractiveCandidateEdge[] BuildCandidateEdges(
        InteractiveFrameContext context,
        IReadOnlySet<int> visibleFaces)
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
            if (visibleFaces.Count > 0 &&
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

    private static float Distance(float x0, float y0, float x1, float y1)
    {
        var dx = x1 - x0;
        var dy = y1 - y0;
        return MathF.Sqrt(dx * dx + dy * dy);
    }
}
