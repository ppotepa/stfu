using STFU.NPR.Rendering;
using STFU.NPR.Composition;
using STFU.Rendering.Abstractions.Requests;
using STFU.Rendering.Abstractions.Surfaces;
using STFU.Rendering.Cpu.Rasterization;
using Xunit;

namespace STFU.Rendering.Cpu.Tests;

public sealed class CpuRasterWorkspaceCacheRegressionTests
{
    [Fact]
    public void GetTiles_ReusesLayoutForSameFrameSizeAndTileSize()
    {
        var workspace = new CpuRasterWorkspace();

        var first = workspace.GetTiles(320, 240, 32);
        var second = workspace.GetTiles(320, 240, 32);
        var third = workspace.GetTiles(320, 240, 64);

        Assert.Equal(first.Count, second.Count);
        Assert.NotEqual(first.Count, third.Count);
        Assert.True(workspace.Counters.TileCacheHits >= 1);
        Assert.True(workspace.Counters.TileCacheMisses >= 2);
    }

    [Fact]
    public void ToneCoordinateMaps_RecordHitAndMissCounters()
    {
        var workspace = new CpuRasterWorkspace();

        var firstX = workspace.GetToneSourceXMap(320, 160);
        var secondX = workspace.GetToneSourceXMap(320, 160);
        var firstY = workspace.GetToneSourceYMap(240, 120);
        var secondY = workspace.GetToneSourceYMap(240, 120);

        Assert.Same(firstX, secondX);
        Assert.Same(firstY, secondY);
        Assert.True(workspace.Counters.ToneSourceCoordCacheMisses >= 2);
        Assert.True(workspace.Counters.ToneSourceCoordCacheHits >= 2);
    }

    [Fact]
    public void DrawToneSurface_SameSizePathRecordsFastPathCounter()
    {
        const int width = 32;
        const int height = 24;
        var target = new PixelSurface(
            width,
            height,
            width * 4,
            PixelSurfaceFormat.Bgra8888Premultiplied,
            new byte[width * height * 4]);
        var rgba = new byte[width * height * 4];
        for (var i = 3; i < rgba.Length; i += 4)
        {
            rgba[i] = 255;
        }

        var tone = new NprToneSurface2D(
            "test",
            "test-layer",
            NprSceneRole.Foreground,
            "tone",
            width,
            height,
            rgba,
            1f);
        var workspace = new CpuRasterWorkspace();
        var rasterizer = new CpuToneRasterizer();

        rasterizer.DrawToneSurface(target, tone, 1f, new NprFrameBudget(), workspace);

        Assert.True(workspace.Counters.TonePixels >= width * height);
        Assert.True(workspace.Counters.ToneSameSizeFastPath >= 1);
    }
}
