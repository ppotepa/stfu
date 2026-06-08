namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed class InteractivePipelineResult
{
    public InteractivePipelineResult(InteractiveFrameDiagnostics diagnostics)
    {
        Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
    }

    public InteractiveFrameDiagnostics Diagnostics { get; }

    public bool RequiresReferenceFallback => Diagnostics.UsedReferenceFallback;
}
