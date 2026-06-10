namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed class InteractiveOutputSummary
{
    public static InteractiveOutputSummary None { get; } = new()
    {
        Kind = InteractiveOutputKind.None,
        Reason = "Interactive Performance did not produce a viewport output candidate."
    };

    public required InteractiveOutputKind Kind { get; init; }

    public bool HasProjectionArtifacts { get; init; }

    public bool HasVisibleFaces { get; init; }

    public bool HasCandidateEdges { get; init; }

    public bool HasStrokeCommands { get; init; }

    public bool HasVisibleStrokeSegments { get; init; }

    public bool HasToneCoverage { get; init; }

    public int ProjectedVertexCount { get; init; }

    public int ProjectedTriangleCount { get; init; }

    public int VisibleFaceCount { get; init; }

    public int CandidateEdgeCount { get; init; }

    public int StrokeCommandCount { get; init; }

    public int VisibleStrokeSegmentCount { get; init; }

    public int ToneRegionCount { get; init; }

    public string Reason { get; init; } = string.Empty;

    public bool IsInteractivePreviewCandidate => Kind == InteractiveOutputKind.InteractivePreviewCandidate;

    public bool HasRenderableStrokeData => HasVisibleStrokeSegments || HasStrokeCommands;
}
