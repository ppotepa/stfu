using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.Strokes;
using Xunit;

namespace STFU.NPR.Pipelines.Tests;

public sealed class InteractiveOutputSelectorTests
{
    [Fact]
    public void Select_reports_no_readiness_for_empty_store()
    {
        var output = InteractiveOutputSelector.Select(new ArtifactStore());

        Assert.Equal(InteractiveOutputKind.ReferenceFallback, output.Summary.Kind);
        Assert.Equal(InteractiveOutputReadiness.None, output.Summary.Readiness);
        Assert.Equal(0, output.Summary.ReadinessScore);
        Assert.False(output.Summary.HasRenderableStrokeData);
    }

    [Fact]
    public void Select_reports_projection_readiness_when_projection_exists()
    {
        var store = new ArtifactStore();
        store.Set(new ProjectedTriangleArtifact
        {
            Key = Key(ArtifactKind.ProjectedTriangles),
            Revision = 1,
            LastBuildTime = TimeSpan.Zero,
            FrontFacingTriangleCount = 1,
            VisibleTriangleCount = 1,
            Triangles =
            [
                new InteractiveProjectedTriangle(
                    SourceIndex: 0,
                    StableId: 1,
                    ProjectedMeshIndex: 0,
                    MeshTriangleIndex: 0,
                    A: 0,
                    B: 1,
                    C: 2,
                    ScreenCenterX: 8,
                    ScreenCenterY: 8,
                    WorldCenterX: 0,
                    WorldCenterY: 0,
                    WorldCenterZ: 0,
                    NormalX: 0,
                    NormalY: 0,
                    NormalZ: 1,
                    Depth: 0.5f,
                    ScreenArea: 10,
                    Shade: 0.5f,
                    IsFrontFacing: true,
                    IsVisible: true)
            ]
        });

        var output = InteractiveOutputSelector.Select(store);

        Assert.Equal(InteractiveOutputKind.ProjectionArtifacts, output.Summary.Kind);
        Assert.Equal(InteractiveOutputReadiness.ProjectionReady, output.Summary.Readiness);
        Assert.Equal(10, output.Summary.ReadinessScore);
    }

    [Fact]
    public void Select_reports_visibility_readiness_when_visible_faces_exist()
    {
        var store = new ArtifactStore();
        store.Set(new VisibleFaceSetArtifact
        {
            Key = Key(ArtifactKind.VisibleFaces),
            Revision = 1,
            LastBuildTime = TimeSpan.Zero,
            FaceCount = 2,
            VisibleFaceCount = 1,
            VisibleFaceIndices = [0],
            ProviderName = "test"
        });

        var output = InteractiveOutputSelector.Select(store);

        Assert.Equal(InteractiveOutputKind.VisibleFaces, output.Summary.Kind);
        Assert.Equal(InteractiveOutputReadiness.VisibilityReady, output.Summary.Readiness);
        Assert.Equal(25, output.Summary.ReadinessScore);
    }

    [Fact]
    public void Select_reports_candidate_edge_readiness_when_candidates_exist()
    {
        var store = new ArtifactStore();
        store.Set(new CandidateEdgeArtifact
        {
            Key = Key(ArtifactKind.CandidateEdges),
            Revision = 1,
            LastBuildTime = TimeSpan.Zero,
            TotalEdgeCount = 4,
            Edges =
            [
                new InteractiveCandidateEdge(
                    SourceEdgeId: 1,
                    FaceA: 0,
                    FaceB: 1,
                    Role: 0,
                    X0: 1,
                    Y0: 1,
                    X1: 20,
                    Y1: 20,
                    ProjectedLength: 28,
                    Depth: 0.5f,
                    Importance: 1)
            ]
        });

        var output = InteractiveOutputSelector.Select(store);

        Assert.Equal(InteractiveOutputKind.CandidateEdges, output.Summary.Kind);
        Assert.Equal(InteractiveOutputReadiness.CandidateEdgesReady, output.Summary.Readiness);
        Assert.Equal(40, output.Summary.ReadinessScore);
    }

    [Fact]
    public void Select_reports_stroke_command_readiness_when_commands_exist()
    {
        var store = new ArtifactStore();
        store.Set(new StrokeCommandArtifact
        {
            Key = Key(ArtifactKind.StrokeCommands),
            Revision = 1,
            LastBuildTime = TimeSpan.Zero,
            SourceCandidateCount = 1,
            Commands =
            [
                new InteractiveStrokeCommand(
                    SourceEdgeId: 1,
                    Role: 0,
                    X0: 1,
                    Y0: 1,
                    X1: 20,
                    Y1: 20,
                    Width: 1,
                    Opacity: 1,
                    Importance: 1,
                    StyleKey: 0)
            ]
        });

        var output = InteractiveOutputSelector.Select(store);

        Assert.Equal(InteractiveOutputKind.StrokeCommands, output.Summary.Kind);
        Assert.Equal(InteractiveOutputReadiness.StrokeCommandsReady, output.Summary.Readiness);
        Assert.Equal(55, output.Summary.ReadinessScore);
        Assert.True(output.Summary.HasRenderableStrokeData);
    }

    [Fact]
    public void Select_reports_segment_readiness_when_visible_segments_exist()
    {
        var store = new ArtifactStore();
        store.Set(new VisibleStrokeSegmentArtifact
        {
            Key = Key(ArtifactKind.VisibleStrokeSegments),
            Revision = 1,
            LastBuildTime = TimeSpan.Zero,
            SourceCommandCount = 1,
            Segments =
            [
                new InteractiveVisibleStrokeSegment(
                    SourceEdgeId: 1,
                    Role: 0,
                    X0: 1,
                    Y0: 1,
                    X1: 20,
                    Y1: 20,
                    Visibility: 1,
                    Importance: 1)
            ]
        });

        var output = InteractiveOutputSelector.Select(store);

        Assert.Equal(InteractiveOutputKind.VisibleStrokeSegments, output.Summary.Kind);
        Assert.Equal(InteractiveOutputReadiness.VisibleSegmentsReady, output.Summary.Readiness);
        Assert.Equal(70, output.Summary.ReadinessScore);
    }

    [Fact]
    public void Select_reports_stroke_frame_readiness_when_interactive_frame_exists_without_tone()
    {
        var store = new ArtifactStore();
        store.Set(CreateStrokeFrameArtifact());

        var output = InteractiveOutputSelector.Select(store);

        Assert.Equal(InteractiveOutputKind.InteractiveStrokeFrame, output.Summary.Kind);
        Assert.Equal(InteractiveOutputReadiness.StrokeFrameReady, output.Summary.Readiness);
        Assert.Equal(85, output.Summary.ReadinessScore);
        Assert.True(output.Summary.IsInteractivePreviewCandidate);
    }

    [Fact]
    public void Select_reports_preview_readiness_when_frame_and_tone_exist()
    {
        var store = new ArtifactStore();
        store.Set(CreateStrokeFrameArtifact());
        store.Set(new ToneCoverageArtifact
        {
            Key = Key(ArtifactKind.ToneCoverage),
            Revision = 1,
            LastBuildTime = TimeSpan.Zero,
            SourceVisibleFaceCount = 1,
            HighlightRegionCount = 0,
            MidtoneRegionCount = 1,
            ShadowRegionCount = 0,
            Regions =
            [
                new InteractiveToneRegion(
                    SourceFaceId: 0,
                    ProjectedMeshIndex: 0,
                    MeshTriangleIndex: 0,
                    Bucket: InteractiveToneBucket.Midtone,
                    ScreenCenterX: 8,
                    ScreenCenterY: 8,
                    ScreenArea: 10,
                    Depth: 0.5f,
                    Shade: 0.5f,
                    CoverageOpacity: 0.25f)
            ]
        });

        var output = InteractiveOutputSelector.Select(store);

        Assert.Equal(InteractiveOutputKind.InteractivePreviewCandidate, output.Summary.Kind);
        Assert.Equal(InteractiveOutputReadiness.PreviewReady, output.Summary.Readiness);
        Assert.Equal(100, output.Summary.ReadinessScore);
        Assert.True(output.Summary.HasCompletePreviewArtifacts);
    }

    private static InteractiveStrokeFrameArtifact CreateStrokeFrameArtifact()
    {
        var segment = new StrokeSegment2D(
            new Point2D(1, 1),
            new Point2D(20, 20),
            StrokeStyle2D.Default);
        var segments = new[] { segment };
        var frame = new StrokeFrame(64, 64, new StrokeSegmentPathList(segments), segments);

        return new InteractiveStrokeFrameArtifact
        {
            Key = Key(ArtifactKind.InteractiveStrokeFrame),
            Revision = 1,
            LastBuildTime = TimeSpan.Zero,
            SourceSegmentCount = 1,
            Frame = frame
        };
    }

    private static ArtifactKey Key(ArtifactKind kind)
    {
        return new ArtifactKey(kind, 1, 1, 1, 64, 64);
    }
}
