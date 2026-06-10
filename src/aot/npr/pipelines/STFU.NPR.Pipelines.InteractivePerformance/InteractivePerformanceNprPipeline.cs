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

        // MVP bridge: keep Reference Quality as a populated graph source, then harvest
        // projection/visibility/stroke/tone artifacts for the Interactive Performance path.
        // IP-012/IP-013 can optionally return an assembled interactive StrokeFrame while
        // the Reference Quality pipeline remains the safe default and export baseline.
        var referenceFrame = _referenceFallback.Execute(context);
        var result = _orchestrator.Execute(intent, context);
        var finalFrame = SelectFinalFrame(context, referenceFrame, result);

        InteractiveDiagnosticsBridge.WriteToContext(context, result.Diagnostics);
        return finalFrame;
    }

    private StrokeFrame SelectFinalFrame(
        NprContext context,
        StrokeFrame referenceFrame,
        InteractivePipelineResult result)
    {
        if (InteractivePreviewPolicy.TrySelectInteractiveFrame(
                _options,
                result,
                out var interactiveFrame,
                out var interactiveReason))
        {
            context.Frame = interactiveFrame;
            result.Diagnostics.ReturnedInteractiveFrame = true;
            result.Diagnostics.ReturnedReferenceFallback = false;
            result.Diagnostics.FinalOutputReason = interactiveReason;
            return interactiveFrame;
        }

        result.Diagnostics.ReturnedInteractiveFrame = false;
        result.Diagnostics.ReturnedReferenceFallback = true;
        result.Diagnostics.FinalOutputReason = interactiveReason;
        if (string.IsNullOrWhiteSpace(result.Diagnostics.FallbackReason))
        {
            result.Diagnostics.FallbackReason = interactiveReason;
        }

        context.Frame = referenceFrame;
        return referenceFrame;
    }
}
