using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;
using STFU.NPR.Pipeline.InteractivePerformance.Providers;
using STFU.NPR.Pipeline.InteractivePerformance.Stages;
using Xunit;

namespace STFU.NPR.Pipelines.Tests;

public sealed class InteractiveProjectionAndVisibleSegmentTests
{
    [Fact]
    public void Projected_vertex_artifact_reports_visible_ratio()
    {
        var artifact = new ProjectedVertexArtifact
        {
            Key = CreateKey(ArtifactKind.ProjectedVertices),
            Revision = 1,
            VisibleVertexCount = 1,
            Vertices =
            [
                new InteractiveProjectedVertex(
                    SourceIndex: 0,
                    MeshVertexIndex: 10,
                    X: 1,
                    Y: 2,
                    Depth: 0.5f,
                    Depth01: 0.7f,
                    IsVisible: true,
                    WorldX: 0,
                    WorldY: 0,
                    WorldZ: 0,
                    NormalX: 0,
                    NormalY: 0,
                    NormalZ: 1,
                    NdcX: 0,
                    NdcY: 0,
                    NdcZ: 0),
                new InteractiveProjectedVertex(
                    SourceIndex: 1,
                    MeshVertexIndex: 11,
                    X: 3,
                    Y: 4,
                    Depth: 0.6f,
                    Depth01: 0.8f,
                    IsVisible: false,
                    WorldX: 1,
                    WorldY: 0,
                    WorldZ: 0,
                    NormalX: 0,
                    NormalY: 0,
                    NormalZ: 1,
                    NdcX: 0,
                    NdcY: 0,
                    NdcZ: 0)
            ]
        };

        Assert.Equal(2, artifact.VertexCount);
        Assert.Equal(1, artifact.VisibleVertexCount);
        Assert.Equal(50d, artifact.VisibleVertexRatioPercent);
    }

    [Fact]
    public void Projected_triangle_artifact_reports_front_facing_and_visible_counts()
    {
        var artifact = new ProjectedTriangleArtifact
        {
            Key = CreateKey(ArtifactKind.ProjectedTriangles),
            Revision = 1,
            FrontFacingTriangleCount = 2,
            VisibleTriangleCount = 1,
            Triangles =
            [
                CreateTriangle(0, frontFacing: true, visible: true),
                CreateTriangle(1, frontFacing: true, visible: false),
                CreateTriangle(2, frontFacing: false, visible: false)
            ]
        };

        Assert.Equal(3, artifact.TriangleCount);
        Assert.Equal(2, artifact.FrontFacingTriangleCount);
        Assert.Equal(1, artifact.VisibleTriangleCount);
        Assert.InRange(artifact.VisibleTriangleRatioPercent, 33.3d, 33.4d);
    }

    [Fact]
    public void Visible_stroke_segment_planner_creates_budgeted_segments()
    {
        var commands = Enumerable.Range(0, 5000)
            .Select(index => new InteractiveStrokeCommand(
                SourceEdgeId: index,
                Role: 0,
                X0: index,
                Y0: 0,
                X1: index + 10,
                Y1: 10,
                Width: 1.5f,
                Opacity: 0.75f,
                Importance: 1f,
                StyleKey: 0))
            .ToArray();

        var fast = VisibleStrokeSegmentPlanner.BuildSegments(commands, InteractiveQualityMode.FastPreview);
        var quality = VisibleStrokeSegmentPlanner.BuildSegments(commands, InteractiveQualityMode.QualityViewport);

        Assert.True(fast.Length <= 4096);
        Assert.True(fast.Length < quality.Length);
        Assert.Equal(commands.Length, quality.Length);
    }

    [Fact]
    public void Artifact_store_returns_latest_artifact_by_kind()
    {
        var store = new ArtifactStore();
        store.Set(new VisibleStrokeSegmentArtifact
        {
            Key = CreateKey(ArtifactKind.VisibleStrokeSegments),
            Revision = 1,
            SourceCommandCount = 1,
            Segments = [new InteractiveVisibleStrokeSegment(1, 0, 0, 0, 10, 10, 1f, 1f)]
        });
        store.Set(new VisibleStrokeSegmentArtifact
        {
            Key = CreateKey(ArtifactKind.VisibleStrokeSegments) with { CameraHash = 42 },
            Revision = 2,
            SourceCommandCount = 2,
            Segments = [new InteractiveVisibleStrokeSegment(2, 0, 0, 0, 20, 20, 1f, 1f)]
        });

        var found = store.TryGetLatest(ArtifactKind.VisibleStrokeSegments, out VisibleStrokeSegmentArtifact latest);

        Assert.True(found);
        Assert.Equal(2, latest.Revision);
        Assert.Equal(2, latest.Segments[0].SourceEdgeId);
    }

    [Fact]
    public void Interactive_output_selector_prefers_preview_candidate_when_strokes_and_tone_exist()
    {
        var store = new ArtifactStore();
        store.Set(new VisibleStrokeSegmentArtifact
        {
            Key = CreateKey(ArtifactKind.VisibleStrokeSegments),
            Revision = 3,
            SourceCommandCount = 1,
            Segments = [new InteractiveVisibleStrokeSegment(4, 0, 0, 0, 10, 10, 1f, 1f)]
        });
        store.Set(new ToneCoverageArtifact
        {
            Key = CreateKey(ArtifactKind.ToneCoverage),
            Revision = 3,
            SourceVisibleFaceCount = 1,
            HighlightRegionCount = 0,
            MidtoneRegionCount = 1,
            ShadowRegionCount = 0,
            Regions =
            [
                new InteractiveToneRegion(
                    SourceFaceId: 1,
                    ProjectedMeshIndex: 0,
                    MeshTriangleIndex: 1,
                    Bucket: InteractiveToneBucket.Midtone,
                    ScreenCenterX: 5,
                    ScreenCenterY: 6,
                    ScreenArea: 24,
                    Depth: 0.5f,
                    Shade: 0.5f,
                    CoverageOpacity: 0.3f)
            ]
        });

        var selection = InteractiveOutputSelector.Select(store);

        Assert.Equal(InteractiveOutputKind.InteractivePreviewCandidate, selection.Summary.Kind);
        Assert.True(selection.Summary.IsInteractivePreviewCandidate);
        Assert.Equal(1, selection.Summary.VisibleStrokeSegmentCount);
        Assert.Equal(1, selection.Summary.ToneRegionCount);
        Assert.NotNull(selection.VisibleStrokeSegments);
        Assert.NotNull(selection.ToneCoverage);
    }

    [Fact]
    public void Visible_stroke_segment_artifact_reports_coverage()
    {
        var artifact = new VisibleStrokeSegmentArtifact
        {
            Key = CreateKey(ArtifactKind.VisibleStrokeSegments),
            Revision = 1,
            SourceCommandCount = 4,
            Segments =
            [
                new InteractiveVisibleStrokeSegment(1, 0, 0, 0, 10, 10, 1f, 1f),
                new InteractiveVisibleStrokeSegment(2, 1, 0, 10, 10, 20, 0.8f, 0.8f)
            ]
        };

        Assert.Equal(2, artifact.SegmentCount);
        Assert.Equal(50d, artifact.SegmentCoveragePercent);
    }


    [Fact]
    public void Projected_triangle_visibility_provider_derives_visible_faces_from_projected_triangles()
    {
        var triangles = new[]
        {
            CreateTriangle(0, frontFacing: true, visible: true),
            CreateTriangle(1, frontFacing: false, visible: true),
            CreateTriangle(2, frontFacing: true, visible: false),
            CreateTriangle(3, frontFacing: true, visible: true)
        };

        var strictVisible = ProjectedTriangleVisibilityProvider.BuildVisibleFaces(triangles, requireFrontFacing: true);
        var permissiveVisible = ProjectedTriangleVisibilityProvider.BuildVisibleFaces(triangles, requireFrontFacing: false);

        Assert.Equal([0, 3], strictVisible);
        Assert.Equal([0, 1, 3], permissiveVisible);
    }

    [Fact]
    public void Projected_artifacts_track_source_kind()
    {
        var vertexArtifact = new ProjectedVertexArtifact
        {
            Key = CreateKey(ArtifactKind.ProjectedVertices),
            Revision = 1,
            VisibleVertexCount = 0,
            Source = InteractiveProjectionSource.ScratchProjection,
            Vertices = []
        };
        var faceArtifact = new VisibleFaceSetArtifact
        {
            Key = CreateKey(ArtifactKind.VisibleFaces),
            Revision = 1,
            FaceCount = 4,
            VisibleFaceCount = 2,
            VisibleFaceIndices = [0, 3],
            Source = InteractiveVisibilitySource.ProjectedTriangles,
            SourceProjectedTriangleCount = 4,
            ProviderName = "ProjectedTriangleVisibility"
        };

        Assert.True(vertexArtifact.IsSelfContained);
        Assert.Equal(InteractiveProjectionSource.ScratchProjection, vertexArtifact.Source);
        Assert.Equal(InteractiveVisibilitySource.ProjectedTriangles, faceArtifact.Source);
        Assert.Equal(50d, faceArtifact.VisibleFaceRatioPercent);
    }

    private static InteractiveProjectedTriangle CreateTriangle(int sourceIndex, bool frontFacing, bool visible)
    {
        return new InteractiveProjectedTriangle(
            SourceIndex: sourceIndex,
            StableId: sourceIndex + 10,
            ProjectedMeshIndex: 0,
            MeshTriangleIndex: sourceIndex,
            A: 0,
            B: 1,
            C: 2,
            ScreenCenterX: 10,
            ScreenCenterY: 12,
            WorldCenterX: 0,
            WorldCenterY: 0,
            WorldCenterZ: 0,
            NormalX: 0,
            NormalY: 0,
            NormalZ: 1,
            Depth: 0.5f,
            ScreenArea: 8f,
            Shade: 0.5f,
            IsFrontFacing: frontFacing,
            IsVisible: visible);
    }

    private static ArtifactKey CreateKey(ArtifactKind kind)
    {
        return new ArtifactKey(
            kind,
            ContentHash: 1,
            CameraHash: 2,
            StyleHash: 3,
            Width: 320,
            Height: 240);
    }
}
