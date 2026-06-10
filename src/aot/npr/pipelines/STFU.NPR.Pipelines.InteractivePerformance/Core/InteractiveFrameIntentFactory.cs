using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;
using STFU.NPR.Pipelines.Abstractions;

namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public static class InteractiveFrameIntentFactory
{
    public static InteractiveFrameIntent FromContext(
        NprContext context,
        FramePipelineStrategyOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(context);
        options ??= FramePipelineStrategyOptions.Default;

        var width = Math.Max(1, context.Width);
        var height = Math.Max(1, context.Height);
        var targetMs = options.TargetFrameMs > 0 ? options.TargetFrameMs : 16.6;
        var signature = InteractiveFrameSignatureFactory.FromContext(context, width, height);

        return new InteractiveFrameIntent(
            FrameId: context.FrameId,
            Width: width,
            Height: height,
            Strategy: FramePipelineStrategy.InteractivePerformance,
            QualityMode: InteractiveQualityMode.Auto,
            FrameBudget: TimeSpan.FromMilliseconds(targetMs),
            CameraChanged: true,
            SceneChanged: true,
            AnimationChanged: false,
            StyleChanged: true,
            ViewportSizeChanged: false,
            DebugOverlayChanged: false,
            Signature: signature)
        {
            Options = options
        };
    }
}