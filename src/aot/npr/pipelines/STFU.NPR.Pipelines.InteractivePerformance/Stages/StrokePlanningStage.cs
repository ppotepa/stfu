using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;
using STFU.NPR.Pipelines.Abstractions;

namespace STFU.NPR.Pipeline.InteractivePerformance.Stages;

public sealed class StrokePlanningStage : IInteractivePipelineStage
{
    private readonly FramePipelineStrategyOptions _options;

    public StrokePlanningStage()
        : this(FramePipelineStrategyOptions.Default)
    {
    }

    public StrokePlanningStage(FramePipelineStrategyOptions options)
    {
        _options = options ?? FramePipelineStrategyOptions.Default;
    }

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
            WriteDiagnostics(context, cached, cached.CommandCount, cached.CommandCount);
            return;
        }

        var sourceCommands = candidateArtifact is null
            ? []
            : StrokeCommandPlanner.BuildCommands(candidateArtifact.Edges);
        var commands = InteractiveBudgetLimiter.LimitStrokeCommands(
            sourceCommands,
            _options.MaxInteractiveStrokeCommands);

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
        WriteDiagnostics(context, artifact, sourceCommands.Length, commands.Length);
    }

    private static CandidateEdgeArtifact? LoadCandidateEdges(InteractiveFrameContext context)
    {
        return context.Artifacts.TryGetLatest(ArtifactKind.CandidateEdges, out CandidateEdgeArtifact candidates)
            ? candidates
            : null;
    }

    private static void WriteDiagnostics(
        InteractiveFrameContext context,
        StrokeCommandArtifact artifact,
        int beforeBudget,
        int afterBudget)
    {
        context.Diagnostics.TotalStrokeCandidates = artifact.SourceCandidateCount;
        context.Diagnostics.StrokeCommands = artifact.CommandCount;
        context.Diagnostics.StrokeCommandReductionPercent = artifact.CommandReductionPercent;
        context.Diagnostics.StrokeCommandsBeforeBudget = beforeBudget;
        context.Diagnostics.StrokeCommandsAfterBudget = afterBudget;
        context.Diagnostics.StrokeCommandBudgetApplied = beforeBudget > afterBudget;
    }
}
