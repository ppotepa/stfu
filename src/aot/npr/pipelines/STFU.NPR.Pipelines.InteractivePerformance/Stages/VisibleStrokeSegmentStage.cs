using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;
using STFU.NPR.Pipelines.Abstractions;

namespace STFU.NPR.Pipeline.InteractivePerformance.Stages;

public sealed class VisibleStrokeSegmentStage : IInteractivePipelineStage
{
    private readonly FramePipelineStrategyOptions _options;

    public VisibleStrokeSegmentStage()
        : this(FramePipelineStrategyOptions.Default)
    {
    }

    public VisibleStrokeSegmentStage(FramePipelineStrategyOptions options)
    {
        _options = options ?? FramePipelineStrategyOptions.Default;
    }

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
            WriteDiagnostics(context, cached, cached.SegmentCount, cached.SegmentCount);
            return;
        }

        var sourceSegments = strokeCommands is null
            ? []
            : VisibleStrokeSegmentPlanner.BuildSegments(
                strokeCommands.Commands,
                context.Intent.QualityMode,
                _options.MaxInteractiveVisibleStrokeSegments);
        var segments = InteractiveBudgetLimiter.LimitVisibleSegments(
            sourceSegments,
            _options.MaxInteractiveVisibleStrokeSegments);

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
        WriteDiagnostics(context, artifact, sourceSegments.Length, segments.Length);
    }

    private static StrokeCommandArtifact? LoadStrokeCommands(InteractiveFrameContext context)
    {
        return context.Artifacts.TryGetLatest(ArtifactKind.StrokeCommands, out StrokeCommandArtifact commands)
            ? commands
            : null;
    }

    private static void WriteDiagnostics(
        InteractiveFrameContext context,
        VisibleStrokeSegmentArtifact artifact,
        int beforeBudget,
        int afterBudget)
    {
        context.Diagnostics.VisibleSegments = artifact.SegmentCount;
        context.Diagnostics.VisibleSegmentSourceCommands = artifact.SourceCommandCount;
        context.Diagnostics.VisibleSegmentCoveragePercent = artifact.SegmentCoveragePercent;
        context.Diagnostics.VisibleSegmentsBeforeBudget = beforeBudget;
        context.Diagnostics.VisibleSegmentsAfterBudget = afterBudget;
        context.Diagnostics.VisibleSegmentBudgetApplied = beforeBudget > afterBudget;
    }
}
