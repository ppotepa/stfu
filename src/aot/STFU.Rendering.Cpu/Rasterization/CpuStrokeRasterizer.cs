using STFU.Rendering.Abstractions.Requests;
using STFU.Rendering.Abstractions.Surfaces;
using STFU.Strokes;

namespace STFU.Rendering.Cpu.Rasterization;

public sealed class CpuStrokeRasterizer
{
    private readonly List<CpuStrokeSegment> _segments = [];
    private List<CpuStrokeSegment>[]? _bins;
    private int _binCount;

    public void DrawPaths(
        PixelSurface target,
        IReadOnlyList<StrokePath2D> paths,
        float opacityScale,
        NprQualityProfile quality,
        NprFrameBudget budget,
        bool preservePathOrder = false)
    {
        if (paths.Count == 0)
        {
            return;
        }

        var segments = CpuStrokeSegmentBuilder.Build(paths, opacityScale, preservePathOrder, _segments);
        DrawSegments(target, segments, quality, budget);
    }

    public void DrawSegments(
        PixelSurface target,
        IReadOnlyList<CpuStrokeSegment> segments,
        NprQualityProfile quality,
        NprFrameBudget budget)
    {
        if (segments.Count == 0)
        {
            return;
        }

        var workerCount = budget.ResolveWorkerCount();
        var parallel = budget.EnableTileParallelism && workerCount > 1;
        var tileSize = Math.Clamp(budget.TileSize, 8, 256);
        var tilesPerRow = Math.Max(1, (target.Width + tileSize - 1) / tileSize);
        var tileRows = Math.Max(1, (target.Height + tileSize - 1) / tileSize);
        var tileCount = tilesPerRow * tileRows;
        var bins = RentBins(tileCount);

        for (var s = 0; s < segments.Count; s++)
        {
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

            var minTileX = Math.Clamp((int)MathF.Floor(segmentMinX / tileSize), 0, tilesPerRow - 1);
            var maxTileX = Math.Clamp((int)MathF.Floor(segmentMaxX / tileSize), 0, tilesPerRow - 1);
            var minTileY = Math.Clamp((int)MathF.Floor(segmentMinY / tileSize), 0, tileRows - 1);
            var maxTileY = Math.Clamp((int)MathF.Floor(segmentMaxY / tileSize), 0, tileRows - 1);

            for (var ty = minTileY; ty <= maxTileY; ty++)
            {
                for (var tx = minTileX; tx <= maxTileX; tx++)
                {
                    bins[ty * tilesPerRow + tx].Add(segment);
                }
            }
        }

        if (!parallel)
        {
            for (var i = 0; i < tileCount; i++)
            {
                DrawTile(target, CreateTile(i, tilesPerRow, tileSize, target.Width, target.Height), bins[i], quality);
            }

            return;
        }

        Parallel.For(
            0,
            tileCount,
            new ParallelOptions { MaxDegreeOfParallelism = workerCount },
            i => DrawTile(target, CreateTile(i, tilesPerRow, tileSize, target.Width, target.Height), bins[i], quality));
    }

    private List<CpuStrokeSegment>[] RentBins(int tileCount)
    {
        if (_bins is null || _bins.Length < tileCount)
        {
            _bins = new List<CpuStrokeSegment>[tileCount];
            _binCount = tileCount;
            for (var i = 0; i < tileCount; i++)
            {
                _bins[i] = [];
            }

            return _bins;
        }

        for (var i = 0; i < _binCount; i++)
        {
            _bins[i].Clear();
        }

        if (_binCount < tileCount)
        {
            for (var i = _binCount; i < tileCount; i++)
            {
                _bins[i] = [];
            }
        }

        _binCount = tileCount;
        return _bins;
    }

    private static CpuTile CreateTile(int index, int tilesPerRow, int tileSize, int targetWidth, int targetHeight)
    {
        var tileX = index % tilesPerRow;
        var tileY = index / tilesPerRow;
        var x = tileX * tileSize;
        var y = tileY * tileSize;
        return new CpuTile(
            x,
            y,
            Math.Min(tileSize, targetWidth - x),
            Math.Min(tileSize, targetHeight - y));
    }

    private static void DrawTile(
        PixelSurface target,
        CpuTile tile,
        IReadOnlyList<CpuStrokeSegment> segments,
        NprQualityProfile quality)
    {
        foreach (var segment in segments)
        {
            DrawSegmentInTile(target, tile, segment, quality);
        }
    }

    private static void DrawSegmentInTile(
        PixelSurface target,
        CpuTile tile,
        CpuStrokeSegment segment,
        NprQualityProfile quality)
    {
        var minX = Math.Max(tile.X, (int)MathF.Floor(segment.MinX));
        var maxX = Math.Min(tile.Right - 1, (int)MathF.Ceiling(segment.MaxX));
        var minY = Math.Max(tile.Y, (int)MathF.Floor(segment.MinY));
        var maxY = Math.Min(tile.Bottom - 1, (int)MathF.Ceiling(segment.MaxY));
        if (maxX < minX || maxY < minY)
        {
            return;
        }

        var ax = segment.Start.X;
        var ay = segment.Start.Y;
        var bx = segment.End.X;
        var by = segment.End.Y;
        var dx = bx - ax;
        var dy = by - ay;
        var lenSq = dx * dx + dy * dy;
        if (lenSq <= 0.000001f)
        {
            return;
        }

        var radius = MathF.Max(0.2f, segment.Thickness * 0.5f);
        var softness = MathF.Max(0.0001f, quality.LineCoverageSoftness);
        var maxDistance = quality.AntialiasLines ? radius + softness : radius;
        var maxDistanceSq = maxDistance * maxDistance;

        for (var y = minY; y <= maxY; y++)
        {
            var py = y + 0.5f;
            for (var x = minX; x <= maxX; x++)
            {
                var px = x + 0.5f;
                var t = ((px - ax) * dx + (py - ay) * dy) / lenSq;
                t = Math.Clamp(t, 0f, 1f);
                var cx = ax + dx * t;
                var cy = ay + dy * t;
                var ddx = px - cx;
                var ddy = py - cy;
                var distanceSq = ddx * ddx + ddy * ddy;
                if (distanceSq >= maxDistanceSq)
                {
                    continue;
                }

                var coverage = quality.AntialiasLines
                    ? Math.Clamp((radius + softness - MathF.Sqrt(distanceSq)) / softness, 0f, 1f)
                    : 1f;

                if (coverage <= 0f)
                {
                    continue;
                }

                CpuPixelBlender.BlendSourceOver(target, x, y, segment.Color, segment.Opacity * coverage);
            }
        }
    }
}
