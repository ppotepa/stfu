using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;

namespace STFU.NPR.Pipeline.InteractivePerformance.Stages;

public sealed class StrokePlanningStage : IInteractivePipelineStage
{
    public string Name => "InteractiveStrokePlanning";

    public bool ShouldRun(InteractiveFrameContext context)
    {
        return context.WorkClass is
            InteractiveWorkClass.StrokeCandidateRefresh or
            InteractiveWorkClass.FullVisibleStrokeRefresh;
    }

    public void Execute(InteractiveFrameContext context)
    {
        var candidateArtifact = LoadCandidateEdges(context);
        var sourceCandidateCount = candidateArtifact?.CandidateEdgeCount ?? 0;
        var key = ArtifactKeyFactory.StrokeCommands(context.Intent, sourceCandidateCount);

        if (context.Artifacts.TryGet<StrokeCommandArtifact>(key, out var cached))
        {
            context.Diagnostics.CacheHits++;
            WriteDiagnostics(context, cached);
            return;
        }

        var commands = candidateArtifact is null
            ? []
            : StrokeCommandPlanner.BuildCommands(candidateArtifact.Edges);

        var artifact = new StrokeCommandArtifact
        {
            Key = key,
            Revision = context.Intent.FrameId,
            LastBuildTime = TimeSpan.Zero,
            SourceCandidateCount = sourceCandidateCount,
            Commands = commands
        };

        context.Artifacts.Set(artifact);
        context.Diagnostics.CacheMisses++;
        WriteDiagnostics(context, artifact);
    }

    private static CandidateEdgeArtifact? LoadCandidateEdges(InteractiveFrameContext context)
    {
        var graph = context.ReferenceContext.Graph;
        var totalEdges = graph.DefaultFragments.Count > 0
            ? graph.DefaultFragments.Count
            : graph.TopologyEdges.Count;
        var key = ArtifactKeyFactory.CandidateEdges(context.Intent, totalEdges);

        return context.Artifacts.TryGet<CandidateEdgeArtifact>(key, out var candidates)
            ? candidates
            : null;
    }

    private static void WriteDiagnostics(InteractiveFrameContext context, StrokeCommandArtifact artifact)
    {
        context.Diagnostics.TotalStrokeCandidates = artifact.SourceCandidateCount;
        context.Diagnostics.StrokeCommands = artifact.CommandCount;
        context.Diagnostics.StrokeCommandReductionPercent = artifact.CommandReductionPercent;
    }
}
