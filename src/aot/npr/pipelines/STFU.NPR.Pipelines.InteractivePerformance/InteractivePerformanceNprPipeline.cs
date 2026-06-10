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
    private readonly InteractiveReferenceExecutionPolicy _referenceExecutionPolicy;

    public InteractivePerformanceNprPipeline()
        : this(FramePipelineStrategyOptions.Default)
    {
    }

    public InteractivePerformanceNprPipeline(FramePipelineStrategyOptions options)
    {
        _options = options ?? FramePipelineStrategyOptions.Default;
        _orchestrator = new InteractiveFrameOrchestrator(_options);
        _referenceFallback = ReferenceQualityPipeline.Create();
        _referenceExecutionPolicy = InteractiveReferenceExecutionPolicy.Resolve(_options);
    }

    public StrokeFrame Execute(NprContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var intent = InteractiveFrameIntentFactory.FromContext(context, _options);

        // Default bridge: keep Reference Quality as a populated graph source, then harvest
        // projection/visibility/stroke/tone artifacts for the Interactive Performance path.
        // The reference execution policy can defer the reference run for explicit preview
        // experiments, while the default remains the safe reference prepass/export baseline.
        var referenceFrame = StrokeFrame.Empty;
        var referenceFrameAvailable = false;
        var referenceExecutedBeforeInteractive = false;

        if (_referenceExecutionPolicy.ExecuteBeforeInteractive)
        {
            referenceFrame = _referenceFallback.Execute(context);
            referenceFrameAvailable = true;
            referenceExecutedBeforeInteractive = true;
        }

        var result = _orchestrator.Execute(intent, context);
        var finalFrame = SelectFinalFrame(
            context,
            result,
            ref referenceFrame,
            ref referenceFrameAvailable,
            referenceExecutedBeforeInteractive);

        InteractiveDiagnosticsBridge.WriteToContext(context, result.Diagnostics);
        return finalFrame;
    }

    private StrokeFrame SelectFinalFrame(
        NprContext context,
        InteractivePipelineResult result,
        ref StrokeFrame referenceFrame,
        ref bool referenceFrameAvailable,
        bool referenceExecutedBeforeInteractive)
    {
        var decision = InteractivePreviewPolicy.Decide(_options, result);
        result.Diagnostics.PreviewDecision = decision.Kind;
        result.Diagnostics.FinalOutputReason = decision.Reason;

        if (decision.SelectedInteractiveFrame && decision.Frame is not null)
        {
            context.Frame = decision.Frame;
            result.Diagnostics.ReturnedInteractiveFrame = true;
            result.Diagnostics.ReturnedReferenceFallback = false;
            result.Diagnostics.ReturnedInteractiveFramePaths = decision.FramePathCount;
            result.Diagnostics.ReturnedInteractiveFrameSegments = decision.FrameSegmentCount;
            result.Diagnostics.CaptureReferenceExecution(
                _referenceExecutionPolicy,
                referenceExecutedBeforeInteractive,
                executedAfterInteractive: false,
                fallbackFrameAvailable: referenceFrameAvailable);
            result.Diagnostics.CaptureOutputHealth(InteractiveOutputHealthAnalyzer.Analyze(result.Diagnostics));
            return decision.Frame;
        }

        var referenceExecutedAfterInteractive = EnsureReferenceFallbackFrame(
            context,
            ref referenceFrame,
            ref referenceFrameAvailable);

        result.Diagnostics.ReturnedInteractiveFrame = false;
        result.Diagnostics.ReturnedReferenceFallback = true;
        result.Diagnostics.ReturnedInteractiveFramePaths = 0;
        result.Diagnostics.ReturnedInteractiveFrameSegments = 0;
        if (string.IsNullOrWhiteSpace(result.Diagnostics.FallbackReason))
        {
            result.Diagnostics.FallbackReason = decision.Reason;
        }

        context.Frame = referenceFrame;
        result.Diagnostics.CaptureReferenceExecution(
            _referenceExecutionPolicy,
            referenceExecutedBeforeInteractive,
            referenceExecutedAfterInteractive,
            referenceFrameAvailable);
        result.Diagnostics.CaptureOutputHealth(InteractiveOutputHealthAnalyzer.Analyze(result.Diagnostics));
        return referenceFrame;
    }

    private bool EnsureReferenceFallbackFrame(
        NprContext context,
        ref StrokeFrame referenceFrame,
        ref bool referenceFrameAvailable)
    {
        if (referenceFrameAvailable)
        {
            return false;
        }

        if (!_referenceExecutionPolicy.AllowLateFallback)
        {
            return false;
        }

        referenceFrame = _referenceFallback.Execute(context);
        referenceFrameAvailable = true;
        return true;
    }
}