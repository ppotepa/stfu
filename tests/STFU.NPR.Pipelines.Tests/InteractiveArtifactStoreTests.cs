using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;
using STFU.NPR.Pipelines.Abstractions;
using Xunit;

namespace STFU.NPR.Pipelines.Tests;

public sealed class InteractiveArtifactStoreTests
{
    private sealed class MockArtifact : IInteractiveArtifact
    {
        public ArtifactKey Key { get; init; }
        public long Revision { get; init; }
        public ArtifactLifetime Lifetime => ArtifactLifetime.FrameOrCamera;
        public TimeSpan LastBuildTime => TimeSpan.Zero;
    }

    private sealed class SceneArtifact : IInteractiveArtifact
    {
        public ArtifactKey Key { get; init; }
        public long Revision { get; init; }
        public ArtifactLifetime Lifetime => ArtifactLifetime.Scene;
        public TimeSpan LastBuildTime => TimeSpan.Zero;
    }

    [Fact]
    public void Store_can_set_and_get_artifact()
    {
        var store = new ArtifactStore();
        var key = new ArtifactKey(ArtifactKind.ProjectedVertices, 1, 1, 1, 800, 600);
        var artifact = new MockArtifact { Key = key, Revision = 42 };

        store.Set(artifact);
        
        Assert.True(store.TryGet<MockArtifact>(key, out var retrieved));
        Assert.Equal(42, retrieved.Revision);
    }

    [Fact]
    public void Store_returns_false_if_missing()
    {
        var store = new ArtifactStore();
        var key = new ArtifactKey(ArtifactKind.ProjectedVertices, 1, 1, 1, 800, 600);

        Assert.False(store.TryGet<MockArtifact>(key, out _));
    }


    [Fact]
    public void RevisionHash_clamps_non_positive_revisions_to_zero()
    {
        Assert.Equal(0UL, ArtifactKey.RevisionHash(-1));
        Assert.Equal(0UL, ArtifactKey.RevisionHash(0));
        Assert.Equal(42UL, ArtifactKey.RevisionHash(42));
    }


    [Fact]
    public void Store_reports_lifetime_stats()
    {
        var store = new ArtifactStore();
        var frameKey = new ArtifactKey(ArtifactKind.ProjectedVertices, 1, 1, 1, 800, 600);
        var sceneKey = new ArtifactKey(ArtifactKind.StaticMeshTopology, 2, 0, 0, 0, 0);

        store.Set(new MockArtifact { Key = frameKey, Revision = 1 });
        store.Set(new SceneArtifact { Key = sceneKey, Revision = 1 });

        var stats = store.SnapshotStats();

        Assert.Equal(2, stats.TotalCount);
        Assert.Equal(1, stats.FrameOrCameraCount);
        Assert.Equal(1, stats.SceneCount);
    }

    [Fact]
    public void ArtifactKeyFactory_uses_signature_hashes()
    {
        var intent = new InteractiveFrameIntent(
            7,
            1280,
            720,
            FramePipelineStrategy.InteractivePerformance,
            InteractiveQualityMode.BalancedViewport,
            TimeSpan.FromMilliseconds(16.6),
            CameraChanged: true,
            SceneChanged: true,
            AnimationChanged: false,
            StyleChanged: true,
            ViewportSizeChanged: false,
            DebugOverlayChanged: false,
            Signature: new InteractiveFrameSignature(
                ContentHash: 11,
                CameraHash: 22,
                StyleHash: 33,
                ViewportHash: 44,
                DebugHash: 55));

        var visibleFaces = ArtifactKeyFactory.VisibleFaces(intent, faceCount: 123);
        var strokeCommands = ArtifactKeyFactory.StrokeCommands(intent, sourceCandidateCount: 40);
        var toneCoverage = ArtifactKeyFactory.ToneCoverage(intent, totalFaceCount: 123, sourceVisibleFaceCount: 80);

        Assert.Equal(ArtifactKind.VisibleFaces, visibleFaces.Kind);
        Assert.Equal(22UL, visibleFaces.CameraHash);
        Assert.Equal(0UL, visibleFaces.StyleHash);
        Assert.NotEqual(11UL, visibleFaces.ContentHash);
        Assert.NotEqual(0UL, strokeCommands.StyleHash);
        Assert.NotEqual(33UL, strokeCommands.StyleHash);
        Assert.NotEqual(0UL, toneCoverage.StyleHash);
    }

    [Fact]
    public void Store_can_cache_projection_summary_artifact()
    {
        var store = new ArtifactStore();
        var key = new ArtifactKey(ArtifactKind.ProjectionSummary, 1, 1, 1, 1280, 720);
        var artifact = new ProjectionSummaryArtifact
        {
            Key = key,
            Revision = 7,
            Width = 1280,
            Height = 720,
            FullProjectionAvailable = false
        };

        store.Set(artifact);

        Assert.True(store.TryGet<ProjectionSummaryArtifact>(key, out var retrieved));
        Assert.Equal(1280, retrieved.Width);
        Assert.False(retrieved.FullProjectionAvailable);
    }
}
