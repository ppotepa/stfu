using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;

namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed class InteractivePipelineResult
{
    public InteractivePipelineResult(InteractiveFrameDiagnostics diagnostics)
        : this(
            diagnostics,
            new InteractiveOutputSelection { Summary = InteractiveOutputSummary.None })
    {
    }

    public InteractivePipelineResult(
        InteractiveFrameDiagnostics diagnostics,
        InteractiveOutputSelection output)
    {
        Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        Output = output ?? throw new ArgumentNullException(nameof(output));
    }

    public InteractiveFrameDiagnostics Diagnostics { get; }

    public InteractiveOutputSelection Output { get; }

    public InteractiveOutputKind OutputKind => Output.Summary.Kind;

    public StrokeCommandArtifact? StrokeCommands => Output.StrokeCommands;

    public VisibleStrokeSegmentArtifact? VisibleStrokeSegments => Output.VisibleStrokeSegments;

    public ToneCoverageArtifact? ToneCoverage => Output.ToneCoverage;

    public bool RequiresReferenceFallback => Diagnostics.UsedReferenceFallback;

    public bool HasInteractivePreviewCandidate => Output.Summary.IsInteractivePreviewCandidate;
}
