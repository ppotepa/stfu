using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;

namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public static class InteractiveOutputSelector
{
    public static InteractiveOutputSelection Select(ArtifactStore artifacts)
    {
        ArgumentNullException.ThrowIfNull(artifacts);

        artifacts.TryGetLatest(ArtifactKind.ProjectedVertices, out ProjectedVertexArtifact? projectedVertices);
        artifacts.TryGetLatest(ArtifactKind.ProjectedTriangles, out ProjectedTriangleArtifact? projectedTriangles);
        artifacts.TryGetLatest(ArtifactKind.VisibleFaces, out VisibleFaceSetArtifact? visibleFaces);
        artifacts.TryGetLatest(ArtifactKind.CandidateEdges, out CandidateEdgeArtifact? candidateEdges);
        artifacts.TryGetLatest(ArtifactKind.StrokeCommands, out StrokeCommandArtifact? strokeCommands);
        artifacts.TryGetLatest(ArtifactKind.VisibleStrokeSegments, out VisibleStrokeSegmentArtifact? visibleStrokeSegments);
        artifacts.TryGetLatest(ArtifactKind.InteractiveStrokeFrame, out InteractiveStrokeFrameArtifact? interactiveStrokeFrame);
        artifacts.TryGetLatest(ArtifactKind.ToneCoverage, out ToneCoverageArtifact? toneCoverage);

        var summary = BuildSummary(
            projectedVertices,
            projectedTriangles,
            visibleFaces,
            candidateEdges,
            strokeCommands,
            visibleStrokeSegments,
            interactiveStrokeFrame,
            toneCoverage);

        return new InteractiveOutputSelection
        {
            Summary = summary,
            ProjectedVertices = projectedVertices,
            ProjectedTriangles = projectedTriangles,
            VisibleFaces = visibleFaces,
            CandidateEdges = candidateEdges,
            StrokeCommands = strokeCommands,
            VisibleStrokeSegments = visibleStrokeSegments,
            InteractiveStrokeFrame = interactiveStrokeFrame,
            ToneCoverage = toneCoverage
        };
    }

    private static InteractiveOutputSummary BuildSummary(
        ProjectedVertexArtifact? projectedVertices,
        ProjectedTriangleArtifact? projectedTriangles,
        VisibleFaceSetArtifact? visibleFaces,
        CandidateEdgeArtifact? candidateEdges,
        StrokeCommandArtifact? strokeCommands,
        VisibleStrokeSegmentArtifact? visibleStrokeSegments,
        InteractiveStrokeFrameArtifact? interactiveStrokeFrame,
        ToneCoverageArtifact? toneCoverage)
    {
        var kind = ResolveKind(
            projectedTriangles,
            visibleFaces,
            candidateEdges,
            strokeCommands,
            visibleStrokeSegments,
            interactiveStrokeFrame,
            toneCoverage);

        var readiness = ResolveReadiness(
            projectedTriangles,
            visibleFaces,
            candidateEdges,
            strokeCommands,
            visibleStrokeSegments,
            interactiveStrokeFrame,
            toneCoverage);

        return new InteractiveOutputSummary
        {
            Kind = kind,
            Readiness = readiness,
            ReadinessScore = ToReadinessScore(readiness),
            HasProjectionArtifacts = projectedVertices is not null || projectedTriangles is not null,
            HasVisibleFaces = visibleFaces is not null,
            HasCandidateEdges = candidateEdges is not null,
            HasStrokeCommands = strokeCommands is not null,
            HasVisibleStrokeSegments = visibleStrokeSegments is not null,
            HasInteractiveStrokeFrame = interactiveStrokeFrame is not null,
            HasToneCoverage = toneCoverage is not null,
            ProjectedVertexCount = projectedVertices?.VertexCount ?? 0,
            ProjectedTriangleCount = projectedTriangles?.TriangleCount ?? 0,
            VisibleFaceCount = visibleFaces?.VisibleFaceCount ?? 0,
            CandidateEdgeCount = candidateEdges?.CandidateEdgeCount ?? 0,
            StrokeCommandCount = strokeCommands?.CommandCount ?? 0,
            VisibleStrokeSegmentCount = visibleStrokeSegments?.SegmentCount ?? 0,
            InteractiveStrokeFramePathCount = interactiveStrokeFrame?.PathCount ?? 0,
            InteractiveStrokeFrameSegmentCount = interactiveStrokeFrame?.FrameSegmentCount ?? 0,
            ToneRegionCount = toneCoverage?.RegionCount ?? 0,
            Reason = BuildReason(kind)
        };
    }

    private static InteractiveOutputKind ResolveKind(
        ProjectedTriangleArtifact? projectedTriangles,
        VisibleFaceSetArtifact? visibleFaces,
        CandidateEdgeArtifact? candidateEdges,
        StrokeCommandArtifact? strokeCommands,
        VisibleStrokeSegmentArtifact? visibleStrokeSegments,
        InteractiveStrokeFrameArtifact? interactiveStrokeFrame,
        ToneCoverageArtifact? toneCoverage)
    {
        if (interactiveStrokeFrame?.HasRenderableFrame == true && (toneCoverage?.RegionCount ?? 0) > 0)
        {
            return InteractiveOutputKind.InteractivePreviewCandidate;
        }

        if (interactiveStrokeFrame?.HasRenderableFrame == true)
        {
            return InteractiveOutputKind.InteractiveStrokeFrame;
        }

        if ((visibleStrokeSegments?.SegmentCount ?? 0) > 0)
        {
            return InteractiveOutputKind.VisibleStrokeSegments;
        }

        if ((toneCoverage?.RegionCount ?? 0) > 0)
        {
            return InteractiveOutputKind.ToneCoverage;
        }

        if ((strokeCommands?.CommandCount ?? 0) > 0)
        {
            return InteractiveOutputKind.StrokeCommands;
        }

        if ((candidateEdges?.CandidateEdgeCount ?? 0) > 0)
        {
            return InteractiveOutputKind.CandidateEdges;
        }

        if ((visibleFaces?.VisibleFaceCount ?? 0) > 0)
        {
            return InteractiveOutputKind.VisibleFaces;
        }

        if ((projectedTriangles?.TriangleCount ?? 0) > 0)
        {
            return InteractiveOutputKind.ProjectionArtifacts;
        }

        return InteractiveOutputKind.ReferenceFallback;
    }

    private static InteractiveOutputReadiness ResolveReadiness(
        ProjectedTriangleArtifact? projectedTriangles,
        VisibleFaceSetArtifact? visibleFaces,
        CandidateEdgeArtifact? candidateEdges,
        StrokeCommandArtifact? strokeCommands,
        VisibleStrokeSegmentArtifact? visibleStrokeSegments,
        InteractiveStrokeFrameArtifact? interactiveStrokeFrame,
        ToneCoverageArtifact? toneCoverage)
    {
        if (interactiveStrokeFrame?.HasRenderableFrame == true && (toneCoverage?.RegionCount ?? 0) > 0)
        {
            return InteractiveOutputReadiness.PreviewReady;
        }

        if (interactiveStrokeFrame?.HasRenderableFrame == true)
        {
            return InteractiveOutputReadiness.StrokeFrameReady;
        }

        if ((visibleStrokeSegments?.SegmentCount ?? 0) > 0)
        {
            return InteractiveOutputReadiness.VisibleSegmentsReady;
        }

        if ((strokeCommands?.CommandCount ?? 0) > 0)
        {
            return InteractiveOutputReadiness.StrokeCommandsReady;
        }

        if ((candidateEdges?.CandidateEdgeCount ?? 0) > 0)
        {
            return InteractiveOutputReadiness.CandidateEdgesReady;
        }

        if ((visibleFaces?.VisibleFaceCount ?? 0) > 0)
        {
            return InteractiveOutputReadiness.VisibilityReady;
        }

        if ((projectedTriangles?.TriangleCount ?? 0) > 0)
        {
            return InteractiveOutputReadiness.ProjectionReady;
        }

        return InteractiveOutputReadiness.None;
    }

    private static int ToReadinessScore(InteractiveOutputReadiness readiness)
    {
        return readiness switch
        {
            InteractiveOutputReadiness.None => 0,
            InteractiveOutputReadiness.ProjectionReady => 10,
            InteractiveOutputReadiness.VisibilityReady => 25,
            InteractiveOutputReadiness.CandidateEdgesReady => 40,
            InteractiveOutputReadiness.StrokeCommandsReady => 55,
            InteractiveOutputReadiness.VisibleSegmentsReady => 70,
            InteractiveOutputReadiness.StrokeFrameReady => 85,
            InteractiveOutputReadiness.PreviewReady => 100,
            _ => 0
        };
    }

    private static string BuildReason(InteractiveOutputKind kind)
    {
        return kind switch
        {
            InteractiveOutputKind.InteractivePreviewCandidate => "Interactive stroke frame and tone coverage are available for viewport preview output.",
            InteractiveOutputKind.InteractiveStrokeFrame => "Interactive stroke frame is available; tone coverage is missing or empty.",
            InteractiveOutputKind.VisibleStrokeSegments => "Interactive visible stroke segments are available, but stroke frame assembly has not produced output.",
            InteractiveOutputKind.ToneCoverage => "Interactive tone coverage is available, but visible stroke segments are missing or empty.",
            InteractiveOutputKind.StrokeCommands => "Interactive stroke commands are available, but visible segment clipping has not produced output.",
            InteractiveOutputKind.CandidateEdges => "Interactive candidate edges are available, but stroke command planning has not produced output.",
            InteractiveOutputKind.VisibleFaces => "Interactive visible faces are available, but edge/stroke planning has not produced output.",
            InteractiveOutputKind.ProjectionArtifacts => "Interactive projection artifacts are available, but visibility/stroke/tone planning has not produced output.",
            InteractiveOutputKind.ReferenceFallback => "Interactive output artifacts are not sufficient yet; Reference Quality remains the final output source.",
            _ => "Interactive output is not available."
        };
    }
}
