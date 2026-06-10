using System.Diagnostics;
using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;
using STFU.NPR.Pipelines.Abstractions;

namespace STFU.NPR.Pipeline.InteractivePerformance.Stages;

public sealed class InteractiveFrameOrchestrator
{
    private readonly FramePipelineStrategyOptions _options;
    private readonly InteractiveFrameScheduler _scheduler = new();
    private readonly AdaptiveBudgetController _budgetController = new();
    private readonly InteractiveFrameChangeTracker _changeTracker = new();
    private readonly ArtifactStore _artifacts = new();
    private InteractiveFrameDiagnostics _previousDiagnostics = new();
    private readonly IInteractivePipelineStage[] _stages;

    public InteractiveFrameOrchestrator()
        : this(FramePipelineStrategyOptions.Default)
    {
    }

    public InteractiveFrameOrchestrator(FramePipelineStrategyOptions options)
    {
        _options = options ?? FramePipelineStrategyOptions.Default;

        var stages = new List<IInteractivePipelineStage>();

        if (_options.EnableProjectionStage)
        {
            stages.Add(new ProjectionStage());
        }

        if (_options.EnableVisibilityStage)
        {
            stages.Add(new VisibilityStage());
        }

        if (_options.EnableCandidateEdgeStage)
        {
            stages.Add(new CandidateEdgeStage());
        }

        if (_options.EnableStrokePlanningStage)
        {
            stages.Add(new StrokePlanningStage());
        }

        if (_options.EnableVisibleStrokeSegmentStage)
        {
            stages.Add(new VisibleStrokeSegmentStage());
        }

        if (_options.EnableTonePlanningStage)
        {
            stages.Add(new TonePlanningStage());
        }

        stages.Add(new ReferenceFallbackStage());

        _stages = stages.ToArray();
    }

    public InteractivePipelineResult Execute(InteractiveFrameIntent intent, NprContext referenceContext)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(referenceContext);

        intent = ResolveIntent(intent);

        var diagnostics = new InteractiveFrameDiagnostics
        {
            Strategy = FramePipelineStrategy.InteractivePerformance
        };
        diagnostics.CaptureIntent(intent);

        var context = new InteractiveFrameContext
        {
            Intent = intent,
            ReferenceContext = referenceContext,
            Artifacts = _artifacts,
            Diagnostics = diagnostics
        };

        context.WorkClass = _scheduler.SelectWork(intent, _previousDiagnostics);
        diagnostics.WorkClass = context.WorkClass;

        foreach (var stage in _stages)
        {
            var elapsed = ExecuteTimed(stage, context);
            diagnostics.AddStageTiming(stage.Name, elapsed);
        }

        var output = SelectOutput();
        diagnostics.CaptureOutput(output.Summary);
        CaptureArtifactStoreStats(diagnostics);
        _previousDiagnostics = diagnostics;

        return new InteractivePipelineResult(diagnostics, output);
    }

    private InteractiveOutputSelection SelectOutput()
    {
        return _options.EnableInteractiveOutputContract
            ? InteractiveOutputSelector.Select(_artifacts)
            : new InteractiveOutputSelection { Summary = InteractiveOutputSummary.None };
    }

    private InteractiveFrameIntent ResolveIntent(InteractiveFrameIntent intent)
    {
        var qualityMode = _budgetController.ResolveQualityMode(intent, _previousDiagnostics);
        intent = intent with { QualityMode = qualityMode };
        return _changeTracker.Resolve(intent);
    }

    private void CaptureArtifactStoreStats(InteractiveFrameDiagnostics diagnostics)
    {
        var stats = _artifacts.SnapshotStats();
        diagnostics.ArtifactStoreItemCount = stats.TotalCount;
        diagnostics.StaticArtifactCount = stats.StaticCount;
        diagnostics.SceneArtifactCount = stats.SceneCount;
        diagnostics.SessionArtifactCount = stats.SessionCount;
        diagnostics.FrameOrCameraArtifactCount = stats.FrameOrCameraCount;
    }

    private static TimeSpan ExecuteTimed(
        IInteractivePipelineStage stage,
        InteractiveFrameContext context)
    {
        if (!stage.ShouldRun(context))
        {
            return TimeSpan.Zero;
        }

        var stopwatch = Stopwatch.StartNew();
        stage.Execute(context);
        stopwatch.Stop();

        return stopwatch.Elapsed;
    }
}
