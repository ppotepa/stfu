using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;

namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed class InteractiveFrameContext
{
    public required InteractiveFrameIntent Intent { get; init; }

    public required NprContext ReferenceContext { get; init; }

    public required ArtifactStore Artifacts { get; init; }

    public required InteractiveFrameDiagnostics Diagnostics { get; init; }

    public InteractiveWorkClass WorkClass { get; set; }
}
