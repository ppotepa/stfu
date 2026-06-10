using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;

namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed class InteractiveOutputSelection
{
    public required InteractiveOutputSummary Summary { get; init; }

    public ProjectedVertexArtifact? ProjectedVertices { get; init; }

    public ProjectedTriangleArtifact? ProjectedTriangles { get; init; }

    public VisibleFaceSetArtifact? VisibleFaces { get; init; }

    public CandidateEdgeArtifact? CandidateEdges { get; init; }

    public StrokeCommandArtifact? StrokeCommands { get; init; }

    public VisibleStrokeSegmentArtifact? VisibleStrokeSegments { get; init; }

    public InteractiveStrokeFrameArtifact? InteractiveStrokeFrame { get; init; }

    public ToneCoverageArtifact? ToneCoverage { get; init; }
}