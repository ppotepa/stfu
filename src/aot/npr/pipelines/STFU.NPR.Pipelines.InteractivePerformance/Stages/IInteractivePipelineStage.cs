using STFU.NPR.Pipeline.InteractivePerformance.Core;

namespace STFU.NPR.Pipeline.InteractivePerformance.Stages;

public interface IInteractivePipelineStage
{
    string Name { get; }

    bool ShouldRun(InteractiveFrameContext context);

    void Execute(InteractiveFrameContext context);
}
