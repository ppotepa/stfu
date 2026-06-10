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

        // MVP bridge: keep Reference Quality as the image source, then harvest
        // populated reference graph artifacts for the Interactive Performance path.
        // Later packages can replace this with direct interactive presentation once
        // projection/visibility/stroke/tone artifacts are self-sufficient.
        var frame = _referenceFallback.Execute(context);
        var result = _orchestrator.Execute(intent, context);

        InteractiveDiagnosticsBridge.WriteToContext(context, result.Diagnostics);

        // IP-011 exposes a typed interactive output contract through diagnostics and
        // InteractivePipelineResult. The final StrokeFrame still comes from Reference
        // Quality until IP-012/IP-013 build and enable a self-contained viewport frame.
        return frame;
    }
}