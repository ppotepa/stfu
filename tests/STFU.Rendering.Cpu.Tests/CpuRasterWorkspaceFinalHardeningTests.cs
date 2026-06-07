using STFU.Rendering.Cpu.Rasterization;
using Xunit;

namespace STFU.Rendering.Cpu.Tests;

public sealed class CpuRasterWorkspaceFinalHardeningTests
{
    [Fact]
    public void GetTiles_ReusesCachedLayout_ForSameDimensionsAndTileSize()
    {
        var workspace = new CpuRasterWorkspace();

        var first = workspace.GetTiles(320, 240, 32);
        var second = workspace.GetTiles(320, 240, 32);

        Assert.Same(first, second);
        Assert.Equal(1, workspace.Counters.TileCacheMisses);
        Assert.Equal(1, workspace.Counters.TileCacheHits);
        Assert.Equal(first.Count, second.Count);
    }

    [Fact]
    public void GetTiles_SeparatesCacheEntries_ByTileSize()
    {
        var workspace = new CpuRasterWorkspace();

        var smallTiles = workspace.GetTiles(320, 240, 16);
        var largeTiles = workspace.GetTiles(320, 240, 64);

        Assert.NotSame(smallTiles, largeTiles);
        Assert.NotEqual(smallTiles.Count, largeTiles.Count);
        Assert.Equal(2, workspace.Counters.TileCacheMisses);
    }

    [Fact]
    public void ToneSourceMaps_ReusesCoordinatesUntilMappingChanges()
    {
        var workspace = new CpuRasterWorkspace();

        var firstX = workspace.GetToneSourceXMap(128, 64);
        var firstY = workspace.GetToneSourceYMap(96, 48);
        var secondX = workspace.GetToneSourceXMap(128, 64);
        var secondY = workspace.GetToneSourceYMap(96, 48);

        Assert.Same(firstX, secondX);
        Assert.Same(firstY, secondY);
        Assert.Equal(2, workspace.Counters.ToneSourceCoordCacheMisses);
        Assert.Equal(2, workspace.Counters.ToneSourceCoordCacheHits);
    }

    [Fact]
    public void ToneScratch_GrowsButDoesNotShrink()
    {
        var workspace = new CpuRasterWorkspace();

        workspace.EnsureToneScratchCapacity(4096);
        var coverage = workspace.ToneCoverageScratch;
        var alpha = workspace.ToneAlphaScratch;
        workspace.EnsureToneScratchCapacity(64);

        Assert.Same(coverage, workspace.ToneCoverageScratch);
        Assert.Same(alpha, workspace.ToneAlphaScratch);
        Assert.True(workspace.ToneCoverageScratch.Length >= 4096);
        Assert.True(workspace.ToneAlphaScratch.Length >= 4096);
    }
}
