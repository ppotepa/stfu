using STFU.NPR.Debug;
using STFU.Parallelism;
using STFU.Rendering.Abstractions.Diagnostics;
using STFU.Rendering.Abstractions.Requests;
using STFU.Rendering.Abstractions.Surfaces;
using STFU.Rendering.Cpu.Rasterization;
using STFU.Strokes;
using Xunit;

namespace STFU.Rendering.Cpu.Tests;

public sealed class CpuStrokeRasterizerTileBinningTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    public void DrawSegments_WorkerCounts_ProduceSamePixels(int workerCount)
    {
        var baseline = RenderHash(1);
        var actual = RenderHash(workerCount);

        Assert.Equal(baseline, actual);
    }

    [Fact]
    public void DrawSegments_SingleThreadDeterministic_MatchesOneWorker()
    {
        var baseline = RenderHash(1);
        var actual = RenderHash(
            1,
            mode: WorkerBudgetMode.SingleThreadDeterministic);

        Assert.Equal(baseline, actual);
    }

    private static ulong RenderHash(int workerCount, WorkerBudgetMode mode = WorkerBudgetMode.Performance)
    {
        const int width = 240;
        const int height = 176;
        var surface = new PixelSurface(
            width,
            height,
            width * 4,
            PixelSurfaceFormat.Bgra8888Premultiplied,
            new byte[width * height * 4]);

        var segments = BuildTileCrossingSegments();

        var rasterizer = new CpuStrokeRasterizer();
        var workspace = new CpuRasterWorkspace();
        var budget = new NprFrameBudget(
            MaxWorkerThreads: workerCount,
            WorkerBudgetMode: mode,
            EnableTileParallelism: true,
            TileSize: 32);

        rasterizer.DrawSegments(surface, segments, NprQualityProfile.Default, budget, workspace);
        return NprRenderParityHasher.HashPixelSurface(surface);
    }

    private static List<CpuStrokeSegment> BuildTileCrossingSegments()
    {
        var segments = new List<CpuStrokeSegment>(96);
        var order = 0;
        for (var i = 0; i < 48; i++)
        {
            var y = 8f + (i % 24) * 7f;
            var slope = (i % 3) * 6f;
            segments.Add(new CpuStrokeSegment(
                new Point2D(4f, y),
                new Point2D(236f, 168f - y * 0.5f + slope),
                new StrokeColor((byte)(40 + i * 3), (byte)(80 + i), (byte)(180 - i)),
                3f + (i % 5),
                0.45f + (i % 4) * 0.1f,
                order++));
        }

        for (var i = 0; i < 48; i++)
        {
            var x = 8f + (i % 24) * 9f;
            var slope = (i % 4) * 5f;
            segments.Add(new CpuStrokeSegment(
                new Point2D(x, 4f),
                new Point2D(236f - x * 0.45f + slope, 172f),
                new StrokeColor((byte)(200 - i), (byte)(50 + i * 2), (byte)(60 + i)),
                2.5f + (i % 6),
                0.38f + (i % 5) * 0.09f,
                order++));
        }

        return segments;
    }
}
