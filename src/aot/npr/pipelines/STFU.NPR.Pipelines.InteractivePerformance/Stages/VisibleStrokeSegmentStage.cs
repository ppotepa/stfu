using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;

namespace STFU.NPR.Pipeline.InteractivePerformance.Stages;

public sealed class VisibleStrokeSegmentStage : IInteractivePipelineStage
{
    public string Name => "InteractiveVisibleStrokeSegments";

    public bool ShouldRun(InteractiveFrameContext context)
    {
        return context.WorkClass is
            InteractiveWorkClass.StrokeCandidateRefresh or
            InteractiveWorkClass.FullVisibleStrokeRefresh;
    }

    public void Execute(InteractiveFrameContext context)
    {
        var strokeCommands = LoadStrokeCommands(context);
        var commandCount = strokeCommands?.CommandCount ?? 0;
        var key = ArtifactKeyFactory.VisibleStrokeSegments(context.Intent, commandCount);

        if (context.Artifacts.TryGet<VisibleStrokeSegmentArtifact>(key, out var cached))
        {
            context.Diagnostics.CacheHits++;
            WriteDiagnostics(context, cached);
            return;
        }

        var segments = strokeCommands is null
            ? []
            : VisibleStrokeSegmentPlanner.BuildSegments(strokeCommands.Commands, context.Intent.QualityMode);

        var artifact = new VisibleStrokeSegmentArtifact
        {
            Key = key,
            Revision = context.Intent.FrameId,
            LastBuildTime = TimeSpan.Zero,
            SourceCommandCount = commandCount,
            Segments = segments,
            Note = strokeCommands is null
                ? "No stroke command artifact was available."
                : "Visible stroke segments built from interactive stroke commands."
        };

        context.Artifacts.Set(artifact);
        context.Diagnostics.CacheMisses++;
        WriteDiagnostics(context, artifact);
    }

    private static StrokeCommandArtifact? LoadStrokeCommands(InteractiveFrameContext context)
    {
        var candidateArtifact = LoadCandidateEdges(context);
        var sourceCandidateCount = candidateArtifact?.CandidateEdgeCount ?? 0;
        var key = ArtifactKeyFactory.StrokeCommands(context.Intent, sourceCandidateCount);

        return context.Artifacts.TryGet<StrokeCommandArtifact>(key, out var commands)
            ? commands
            : null;
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

    private static void WriteDiagnostics(InteractiveFrameContext context, VisibleStrokeSegmentArtifact artifact)
    {
        context.Diagnostics.VisibleSegments = artifact.SegmentCount;
        context.Diagnostics.VisibleSegmentSourceCommands = artifact.SourceCommandCount;
        context.Diagnostics.VisibleSegmentCoveragePercent = artifact.SegmentCoveragePercent;
    }
}
