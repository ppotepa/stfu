using STFU.NPR.Pipeline;

namespace STFU.NPR.Steps.Debug;

public sealed class CaptureFrameHistoryStep : INprStep
{
    public void Execute(NprContext context)
    {
        context.FrameHistoryState.Capture(context.View, context.Graph, context.Frame, context.TimeSeconds);
    }
}
