using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using Xunit;

namespace STFU.NPR.Pipelines.Tests;

public sealed class InteractiveArtifactStorePruningTests
{
    private sealed class TestArtifact : IInteractiveArtifact
    {
        public required ArtifactKey Key { get; init; }

        public required long Revision { get; init; }

        public ArtifactLifetime Lifetime { get; init; } = ArtifactLifetime.FrameOrCamera;

        public TimeSpan LastBuildTime => TimeSpan.Zero;
    }

    [Fact]
    public void PruneFrameOrCameraArtifacts_keeps_latest_items_per_kind()
    {
        var store = new ArtifactStore();
        for (var revision = 1; revision <= 5; revision++)
        {
            store.Set(CreateArtifact(ArtifactKind.ProjectedVertices, revision));
        }

        for (var revision = 1; revision <= 4; revision++)
        {
            store.Set(CreateArtifact(ArtifactKind.VisibleFaces, revision));
        }

        var sceneArtifact = CreateArtifact(ArtifactKind.StaticMeshTopology, revision: 99, ArtifactLifetime.Scene);
        store.Set(sceneArtifact);

        var removed = store.PruneFrameOrCameraArtifacts(maxPerKind: 2, maxTotal: 0);

        Assert.Equal(5, removed);
        Assert.Equal(5, store.Count);
        Assert.True(store.TryGet<TestArtifact>(Key(ArtifactKind.ProjectedVertices, 4), out _));
        Assert.True(store.TryGet<TestArtifact>(Key(ArtifactKind.ProjectedVertices, 5), out _));
        Assert.True(store.TryGet<TestArtifact>(Key(ArtifactKind.VisibleFaces, 3), out _));
        Assert.True(store.TryGet<TestArtifact>(Key(ArtifactKind.VisibleFaces, 4), out _));
        Assert.True(store.TryGet<TestArtifact>(sceneArtifact.Key, out _));
        Assert.False(store.TryGet<TestArtifact>(Key(ArtifactKind.ProjectedVertices, 1), out _));
    }

    [Fact]
    public void PruneFrameOrCameraArtifacts_can_apply_global_frame_artifact_cap()
    {
        var store = new ArtifactStore();
        for (var revision = 1; revision <= 4; revision++)
        {
            store.Set(CreateArtifact(ArtifactKind.ProjectedVertices, revision));
            store.Set(CreateArtifact(ArtifactKind.VisibleFaces, revision));
            store.Set(CreateArtifact(ArtifactKind.CandidateEdges, revision));
        }

        var removed = store.PruneFrameOrCameraArtifacts(maxPerKind: 0, maxTotal: 5);
        var stats = store.SnapshotStats();

        Assert.Equal(7, removed);
        Assert.Equal(5, stats.FrameOrCameraCount);
        Assert.True(store.TryGet<TestArtifact>(Key(ArtifactKind.ProjectedVertices, 4), out _));
        Assert.True(store.TryGet<TestArtifact>(Key(ArtifactKind.VisibleFaces, 4), out _));
        Assert.True(store.TryGet<TestArtifact>(Key(ArtifactKind.CandidateEdges, 4), out _));
        Assert.False(store.TryGet<TestArtifact>(Key(ArtifactKind.ProjectedVertices, 1), out _));
    }

    [Fact]
    public void PruneFrameOrCameraArtifacts_returns_zero_when_store_is_already_under_budget()
    {
        var store = new ArtifactStore();
        store.Set(CreateArtifact(ArtifactKind.ProjectedVertices, revision: 10));
        store.Set(CreateArtifact(ArtifactKind.VisibleFaces, revision: 11));
        store.Set(CreateArtifact(ArtifactKind.StaticMeshTopology, revision: 12, ArtifactLifetime.Scene));

        var removed = store.PruneFrameOrCameraArtifacts(maxPerKind: 4, maxTotal: 16);
        var stats = store.SnapshotStats();

        Assert.Equal(0, removed);
        Assert.Equal(3, store.Count);
        Assert.Equal(2, stats.FrameOrCameraCount);
        Assert.Equal(1, stats.SceneCount);
    }

    private static TestArtifact CreateArtifact(
        ArtifactKind kind,
        long revision,
        ArtifactLifetime lifetime = ArtifactLifetime.FrameOrCamera)
    {
        return new TestArtifact
        {
            Key = Key(kind, revision),
            Revision = revision,
            Lifetime = lifetime
        };
    }

    private static ArtifactKey Key(ArtifactKind kind, long revision)
    {
        return new ArtifactKey(
            kind,
            ContentHash: (ulong)(1000 + revision),
            CameraHash: (ulong)(2000 + revision),
            StyleHash: (ulong)(3000 + revision),
            Width: 320,
            Height: 240);
    }
}
