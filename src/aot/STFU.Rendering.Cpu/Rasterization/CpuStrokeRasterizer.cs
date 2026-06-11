using STFU.Common.Math;
using STFU.Parallelism;
using STFU.Rendering.Abstractions.Requests;
using STFU.Rendering.Abstractions.Surfaces;
using STFU.Strokes;

namespace STFU.Rendering.Cpu.Rasterization;

public sealed class CpuStrokeRasterizer
{
    public void DrawPaths(
        PixelSurface target,
        IReadOnlyList<StrokePath2D> paths,
        float opacityScale,
        NprQualityProfile quality,
        NprFrameBudget budget,
        CpuRasterWorkspace workspace,
        CancellationToken cancellationToken = default,
        bool preservePathOrder = false)
    {
        if (paths.Count == 0)
        {
            return;
        }

        var segments = CpuStrokeSegmentBuilder.Build(
            paths,
            opacityScale,
            preservePathOrder,
            workspace.Segments,
            workspace.PathSortScratch);
        DrawSegments(target, segments, quality, budget, workspace, cancellationToken);
    }

    public void DrawStrokeSegments(
        PixelSurface target,
        IReadOnlyList<StrokeSegment2D> segments,
        float opacityScale,
        NprQualityProfile quality,
        NprFrameBudget budget,
        CpuRasterWorkspace workspace,
        CancellationToken cancellationToken = default)
    {
        if (segments.Count == 0)
        {
            return;
        }

        var output = workspace.Segments;
        output.Clear();
        output.EnsureCapacity(segments.Count);
        var order = 0;

        for (var i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            var opacity = NumericMath.Clamp01(segment.Style.Opacity * opacityScale);
            output.Add(new CpuStrokeSegment(
                segment.Start,
                segment.End,
                segment.Style.Color,
                segment.Style.Thickness,
                opacity,
                order++));
        }

        DrawSegments(target, output, quality, budget, workspace, cancellationToken);
    }

    public void DrawSegments(
        PixelSurface target,
        IReadOnlyList<CpuStrokeSegment> segments,
        NprQualityProfile quality,
        NprFrameBudget budget,
        CpuRasterWorkspace workspace,
        CancellationToken cancellationToken = default)
    {
        if (segments.Count == 0)
        {
            return;
        }

        workspace.Counters.StrokeSegmentsInput += segments.Count;

        var workerCount = budget.ResolveWorkerCount();
        var tileSize = RasterMath.ClampTileSize(budget.TileSize);
        var tilesPerRow = RasterMath.TilesPerAxis(target.Width, tileSize);
        var tileRows = RasterMath.TilesPerAxis(target.Height, tileSize);
        var tileCount = tilesPerRow * tileRows;
        var tiles = workspace.GetTiles(target.Width, target.Height, tileSize);
        var parallel = budget.EnableTileParallelism && workerCount > 1 && segments.Count >= 64 && tileCount > 1;
        var rangeCount = DeterministicParallel.GetRangeCount(segments.Count, workerCount, 64);
        workspace.Counters.StrokeTileCount = tileCount;

        if (!parallel || workerCount <= 1 || rangeCount <= 1)
        {
            DrawSegmentsSequentialWithTileBins(
                target,
                segments,
                quality,
                workspace,
                tileSize,
                tilesPerRow,
                tileRows,
                tileCount,
                tiles);
            return;
        }

        workspace.EnsureTileBinningCapacity(rangeCount, tileCount, segments.Count * 4);
        Array.Clear(workspace.RangeTileCounts, 0, rangeCount * tileCount);
        Array.Clear(workspace.TileCounts, 0, tileCount);
        Array.Clear(workspace.TileOffsets, 0, tileCount);
        Array.Clear(workspace.RangeTileOffsets, 0, rangeCount * tileCount);
        Array.Clear(workspace.TileWriteCursors, 0, rangeCount * tileCount);

        DeterministicParallel.ForRanges(
            0,
            segments.Count,
            workerCount,
            cancellationToken,
            (start, end, rangeIndex, token) =>
            {
                token.ThrowIfCancellationRequested();
                var rangeBase = rangeIndex * tileCount;
                for (var s = start; s < end; s++)
                {
                    if ((s & 0x3FF) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    var segment = segments[s];
                    var segmentMinX = segment.MinX;
                    var segmentMaxX = segment.MaxX;
                    var segmentMinY = segment.MinY;
                    var segmentMaxY = segment.MaxY;
                    if (segmentMaxX < 0 ||
                        segmentMaxY < 0 ||
                        segmentMinX >= target.Width ||
                        segmentMinY >= target.Height)
                    {
                        continue;
                    }

                    var minTileX = RasterMath.TileIndexFromCoordinate(segmentMinX, tileSize, tilesPerRow);
                    var maxTileX = RasterMath.TileIndexFromCoordinate(segmentMaxX, tileSize, tilesPerRow);
                    var minTileY = RasterMath.TileIndexFromCoordinate(segmentMinY, tileSize, tileRows);
                    var maxTileY = RasterMath.TileIndexFromCoordinate(segmentMaxY, tileSize, tileRows);

                    for (var ty = minTileY; ty <= maxTileY; ty++)
                    {
                        var tileRowBase = ty * tilesPerRow;
                        for (var tx = minTileX; tx <= maxTileX; tx++)
                        {
                            workspace.RangeTileCounts[rangeBase + tileRowBase + tx]++;
                        }
                    }
                }
            },
            minItemsPerRange: 64);

        for (var tileIndex = 0; tileIndex < tileCount; tileIndex++)
        {
            var total = 0;
            for (var rangeIndex = 0; rangeIndex < rangeCount; rangeIndex++)
            {
                total += workspace.RangeTileCounts[rangeIndex * tileCount + tileIndex];
            }

            workspace.TileCounts[tileIndex] = total;
        }

        var totalRefs = PrefixSums.ExclusiveFromCounts(
            workspace.TileCounts.AsSpan(0, tileCount),
            workspace.TileOffsets.AsSpan(0, tileCount));
        workspace.EnsureTileBinningCapacity(rangeCount, tileCount, totalRefs);
        workspace.Counters.StrokeTileRefs += totalRefs; // stroke.tileRefs
        workspace.Counters.StrokeTilesTouched += CountTouchedTiles(workspace.TileCounts, tileCount);
        workspace.Counters.StrokeMaxRefsPerTile = CountMaxRefs(workspace.TileCounts, tileCount); // stroke.maxRefsPerTile
        Array.Clear(workspace.TileSegmentIndices, 0, totalRefs);

        for (var tileIndex = 0; tileIndex < tileCount; tileIndex++)
        {
            var cursor = workspace.TileOffsets[tileIndex];
            for (var rangeIndex = 0; rangeIndex < rangeCount; rangeIndex++)
            {
                var count = workspace.RangeTileCounts[rangeIndex * tileCount + tileIndex];
                workspace.RangeTileOffsets[rangeIndex * tileCount + tileIndex] = cursor;
                cursor += count;
            }
        }

        Array.Copy(workspace.RangeTileOffsets, workspace.TileWriteCursors, workspace.RangeTileOffsets.Length);
        DeterministicParallel.ForRanges(
            0,
            segments.Count,
            workerCount,
            cancellationToken,
            (start, end, rangeIndex, token) =>
            {
                token.ThrowIfCancellationRequested();
                var rangeBase = rangeIndex * tileCount;
                for (var s = start; s < end; s++)
                {
                    if ((s & 0x3FF) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    var segment = segments[s];
                    var segmentMinX = segment.MinX;
                    var segmentMaxX = segment.MaxX;
                    var segmentMinY = segment.MinY;
                    var segmentMaxY = segment.MaxY;
                    if (segmentMaxX < 0 ||
                        segmentMaxY < 0 ||
                        segmentMinX >= target.Width ||
                        segmentMinY >= target.Height)
                    {
                        continue;
                    }

                    var minTileX = RasterMath.TileIndexFromCoordinate(segmentMinX, tileSize, tilesPerRow);
                    var maxTileX = RasterMath.TileIndexFromCoordinate(segmentMaxX, tileSize, tilesPerRow);
                    var minTileY = RasterMath.TileIndexFromCoordinate(segmentMinY, tileSize, tileRows);
                    var maxTileY = RasterMath.TileIndexFromCoordinate(segmentMaxY, tileSize, tileRows);

                    for (var ty = minTileY; ty <= maxTileY; ty++)
                    {
                        var tileRowBase = ty * tilesPerRow;
                        for (var tx = minTileX; tx <= maxTileX; tx++)
                        {
                            var tileIndex = tileRowBase + tx;
                            var writeIndex = workspace.TileWriteCursors[rangeBase + tileIndex]++;
                            workspace.TileSegmentIndices[writeIndex] = s;
                        }
                    }
                }
            },
            minItemsPerRange: 64);

        DeterministicParallel.ForRanges(
            0,
            tileCount,
            workerCount,
            cancellationToken,
            (start, end, _, token) =>
            {
                token.ThrowIfCancellationRequested();
                for (var i = start; i < end; i++)
                {
                    if ((i & 0x3FF) == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    DrawTile(
                        target,
                        tiles[i],
                        segments,
                        workspace.TileSegmentIndices,
                        workspace.TileOffsets[i],
                        workspace.TileCounts[i],
                        quality,
                        workspace.Counters);
                }
            },
            minItemsPerRange: 1);
    }

    private static void DrawSegmentsSequentialWithTileBins(
        PixelSurface target,
        IReadOnlyList<CpuStrokeSegment> segments,
        NprQualityProfile quality,
        CpuRasterWorkspace workspace,
        int tileSize,
        int tilesPerRow,
        int tileRows,
        int tileCount,
        IReadOnlyList<CpuTile> tiles)
    {
        workspace.EnsureTileBinningCapacity(1, tileCount, segments.Count);
        Array.Clear(workspace.TileCounts, 0, tileCount);
        Array.Clear(workspace.TileOffsets, 0, tileCount);

        for (var segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
        {
            if (!TryGetSegmentTileRange(
                    target,
                    segments[segmentIndex],
                    tileSize,
                    tilesPerRow,
                    tileRows,
                    out var minTileX,
                    out var maxTileX,
                    out var minTileY,
                    out var maxTileY))
            {
                continue;
            }

            for (var ty = minTileY; ty <= maxTileY; ty++)
            {
                var tileRowBase = ty * tilesPerRow;
                for (var tx = minTileX; tx <= maxTileX; tx++)
                {
                    workspace.TileCounts[tileRowBase + tx]++;
                }
            }
        }

        var totalRefs = PrefixSums.ExclusiveFromCounts(
            workspace.TileCounts.AsSpan(0, tileCount),
            workspace.TileOffsets.AsSpan(0, tileCount));
        workspace.EnsureTileBinningCapacity(1, tileCount, totalRefs);
        workspace.Counters.StrokeTileRefs += totalRefs;
        workspace.Counters.StrokeTilesTouched += CountTouchedTiles(workspace.TileCounts, tileCount);
        workspace.Counters.StrokeMaxRefsPerTile = CountMaxRefs(workspace.TileCounts, tileCount);
        Array.Copy(workspace.TileOffsets, workspace.TileWriteCursors, tileCount);

        for (var segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
        {
            if (!TryGetSegmentTileRange(
                    target,
                    segments[segmentIndex],
                    tileSize,
                    tilesPerRow,
                    tileRows,
                    out var minTileX,
                    out var maxTileX,
                    out var minTileY,
                    out var maxTileY))
            {
                continue;
            }

            for (var ty = minTileY; ty <= maxTileY; ty++)
            {
                var tileRowBase = ty * tilesPerRow;
                for (var tx = minTileX; tx <= maxTileX; tx++)
                {
                    var tileIndex = tileRowBase + tx;
                    workspace.TileSegmentIndices[workspace.TileWriteCursors[tileIndex]++] = segmentIndex;
                }
            }
        }

        for (var tileIndex = 0; tileIndex < tileCount; tileIndex++)
        {
            DrawTile(
                target,
                tiles[tileIndex],
                segments,
                workspace.TileSegmentIndices,
                workspace.TileOffsets[tileIndex],
                workspace.TileCounts[tileIndex],
                quality,
                workspace.Counters);
        }
    }

    private static bool TryGetSegmentTileRange(
        PixelSurface target,
        CpuStrokeSegment segment,
        int tileSize,
        int tilesPerRow,
        int tileRows,
        out int minTileX,
        out int maxTileX,
        out int minTileY,
        out int maxTileY)
    {
        minTileX = maxTileX = minTileY = maxTileY = 0;
        if (segment.MaxX < 0 ||
            segment.MaxY < 0 ||
            segment.MinX >= target.Width ||
            segment.MinY >= target.Height)
        {
            return false;
        }

        minTileX = RasterMath.TileIndexFromCoordinate(segment.MinX, tileSize, tilesPerRow);
        maxTileX = RasterMath.TileIndexFromCoordinate(segment.MaxX, tileSize, tilesPerRow);
        minTileY = RasterMath.TileIndexFromCoordinate(segment.MinY, tileSize, tileRows);
        maxTileY = RasterMath.TileIndexFromCoordinate(segment.MaxY, tileSize, tileRows);
        return true;
    }

    private static void DrawTile(
        PixelSurface target,
        CpuTile tile,
        IReadOnlyList<CpuStrokeSegment> segments,
        int[] segmentIndices,
        int segmentOffset,
        int segmentCount,
        NprQualityProfile quality,
        CpuRasterizationCounters counters)
    {
        for (var index = 0; index < segmentCount; index++)
        {
            DrawSegmentInTile(target, tile, segments[segmentIndices[segmentOffset + index]], quality, counters);
        }
    }

    private static void DrawSegmentInTile(
        PixelSurface target,
        CpuTile tile,
        CpuStrokeSegment segment,
        NprQualityProfile quality,
        CpuRasterizationCounters counters)
    {
        var (minX, maxX) = RasterMath.ClampPixelRange(segment.MinX, segment.MaxX, tile.X, tile.Right);
        var (minY, maxY) = RasterMath.ClampPixelRange(segment.MinY, segment.MaxY, tile.Y, tile.Bottom);
        if (maxX < minX || maxY < minY)
        {
            return;
        }

        var ax = segment.Start.X;
        var ay = segment.Start.Y;
        var bx = segment.End.X;
        var by = segment.End.Y;
        if (!StrokeRasterCoverageMath.TryProjectPointToSegment(ax, ay, bx, by, ax, ay, out _))
        {
            return;
        }

        var radius = NumericMath.AtLeast(segment.Thickness * 0.5f, 0.2f);
        var softness = NumericMath.AtLeast(quality.LineCoverageSoftness, 0.0001f);
        var maxDistance = quality.AntialiasLines ? radius + softness : radius;
        var maxDistanceSq = maxDistance * maxDistance;

        long pixelTests = 0;
        long pixelWrites = 0;

        for (var y = minY; y <= maxY; y++)
        {
            var py = y + 0.5f;
            for (var x = minX; x <= maxX; x++)
            {
                var px = x + 0.5f;
                pixelTests++;
                if (!StrokeRasterCoverageMath.TryProjectPointToSegment(ax, ay, bx, by, px, py, out var projection) ||
                    projection.DistanceSquared >= maxDistanceSq)
                {
                    continue;
                }

                var coverage = StrokeRasterCoverageMath.Coverage(
                    projection.DistanceSquared,
                    radius,
                    softness,
                    quality.AntialiasLines);

                if (coverage <= 0f)
                {
                    continue;
                }

                CpuPixelBlender.BlendSourceOver(target, x, y, segment.Color, segment.Opacity * coverage);
                pixelWrites++;
            }
        }

        if (pixelTests > 0)
        {
            Interlocked.Add(ref counters.StrokePixelTests, pixelTests);
        }

        if (pixelWrites > 0)
        {
            Interlocked.Add(ref counters.StrokePixelWrites, pixelWrites);
        }
    }

    private static int CountTouchedTiles(int[] tileCounts, int tileCount)
    {
        var touched = 0;
        for (var i = 0; i < tileCount; i++)
        {
            if (tileCounts[i] > 0)
            {
                touched++;
            }
        }

        return touched;
    }

    private static int CountMaxRefs(int[] tileCounts, int tileCount)
    {
        var maxRefs = 0;
        for (var i = 0; i < tileCount; i++)
        {
            maxRefs = NumericMath.AtLeast(maxRefs, tileCounts[i]);
        }

        return maxRefs;
    }
}
