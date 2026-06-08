using STFU.NPR.Pipeline.InteractivePerformance.Core;

namespace STFU.NPR.Pipeline.InteractivePerformance.Stages;

public sealed class ReferenceFallbackStage : IInteractivePipelineStage
{
    public string Name => "ReferenceFallback";

    public bool ShouldRun(InteractiveFrameContext context)
    {
        return true;
    }

    public void Execute(InteractiveFrameContext context)
    {
        context.Diagnostics.UsedReferenceFallback = true;

        if (string.IsNullOrWhiteSpace(context.Diagnostics.FallbackReason))
        {
            context.Diagnostics.FallbackReason =
                "Interactive performance path currently delegates to Reference Quality for full feature parity.";
        }
    }
}
