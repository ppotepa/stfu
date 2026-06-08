using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
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
