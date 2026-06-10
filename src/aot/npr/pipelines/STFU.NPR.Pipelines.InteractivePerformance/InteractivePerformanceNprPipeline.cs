using STFU.NPR.Pipeline;
using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.NPR.Pipeline.InteractivePerformance.Stages;
using STFU.NPR.Pipeline.ReferenceQuality;
using STFU.NPR.Pipelines.Abstractions;
using STFU.Strokes;

namespace STFU.NPR.Pipeline.InteractivePerformance;

public sealed class InteractivePerformanceNprPipeline : INprPipeline
{
    private readonly FramePipelineStrategyOptions _options;
    private readonly InteractiveFrameOrchestrator _orchestrator;
    private readonly INprPipeline _referenceFallback;

    public InteractivePerformanceNprPipeline()
        : this(FramePipelineStrategyOptions.Default)
    {
    }

    public InteractivePerformanceNprPipeline(FramePipelineStrategyOptions options)
    {
        _options = options ?? FramePipelineStrategyOptions.Default;
        _orchestrator = new InteractiveFrameOrchestrator(_options);
        _referenceFallback = ReferenceQualityPipeline.Create();
    }

    public StrokeFrame Execute(NprContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var intent = InteractiveFrameIntentFactory.FromContext(context, _options);

        var result = _orchestrator.Execute(intent, context);
        var frame = _referenceFallback.Execute(context);

        InteractiveDiagnosticsBridge.WriteToContext(context, result.Diagnostics);

        return frame;
    }
}