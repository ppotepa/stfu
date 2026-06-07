using STFU.Common.Math;
using STFU.NPR.Graph;
using STFU.Parallelism;
using System.Runtime.InteropServices;

namespace STFU.NPR.Pipeline.Default.Steps;

public sealed class DefaultBuildFaceIdVisibilityBufferStep : STFU.NPR.Pipeline.INprStep
{
    private const int VisibilityTileSize = 32;
    private DefaultFaceIdVisibilityBuffer? _buffer;
    private TriangleRasterInfo[] _triangleInfos = [];
    private int[] _rangeTileCounts = [];
    private int[] _rangeTileOffsets = [];
    private int[] _tileCounts = [];
    private int[] _tileOffsets = [];
    private int[] _tileWriteCursors = [];
    private int[] _tileTriangleIndices = [];
    private long[] _tilePixelTests = [];
    private long[] _tilePixelWrites = [];
    private int _lastWidth;
    private int _lastHeight;
    private int _lastTileCountX;
    private int _lastTileCountY;
    private int _lastTileCount;

    public void Execute(STFU.NPR.Pipeline.NprContext context)
    {
        var drawing = context.Settings.DefaultDrawing;
        var width = RasterMath.AtLeastPixels((int)NumericMath.Floor(context.Width * drawing.DepthScale), 8);
        var height = RasterMath.AtLeastPixels((int)NumericMath.Floor(context.Height * drawing.DepthScale), 8);

        var buffer = RentBuffer(width, height, context.Graph.Triangles.Count);
        context.Graph.DefaultFaceIdVisibility = buffer;
        var triangleCount = context.Graph.Triangles.Count;
        context.Counters.Set("DefaultBuildFaceIdVisibilityBufferStep.sourceTriangles", triangleCount);

        if (!drawing.OcclusionCulling)
        {
            Array.Fill(buffer.FaceVisible, true);
            context.Counters.Set("DefaultBuildFaceIdVisibilityBufferStep.tileCount", 0);
            context.Counters.Set("DefaultBuildFaceIdVisibilityBufferStep.rangeCount", 0);
            context.Counters.Set("DefaultBuildFaceIdVisibilityBufferStep.totalTileRefs", 0);
            context.Counters.Set("DefaultBuildFaceIdVisibilityBufferStep.validRasterInfoCount", 0);
            context.Counters.Set("DefaultBuildFaceIdVisibilityBufferStep.maxRefsPerTile", 0);
            context.Counters.Set("DefaultBuildFaceIdVisibilityBufferStep.pixelTests", 0);
            context.Counters.Set("DefaultBuildFaceIdVisibilityBufferStep.pixelWrites", 0);
            context.Counters.Set("DefaultBuildFaceIdVisibilityBufferStep.visibleFaces", triangleCount);
            return;
        }

        buffer.Clear();
        if (triangleCount == 0)
        {
            buffer.MarkVisibleFaces(clear: false);
            context.Counters.Set("DefaultBuildFaceIdVisibilityBufferStep.tileCount", 0);
            context.Counters.Set("DefaultBuildFaceIdVisibilityBufferStep.rangeCount", 0);
            context.Counters.Set("DefaultBuildFaceIdVisibilityBufferStep.totalTileRefs", 0);
            context.Counters.Set("DefaultBuildFaceIdVisibilityBufferStep.validRasterInfoCount", 0);
            context.Counters.Set("DefaultBuildFaceIdVisibilityBufferStep.maxRefsPerTile", 0);
            context.Counters.Set("DefaultBuildFaceIdVisibilityBufferStep.pixelTests", 0);
            context.Counters.Set("DefaultBuildFaceIdVisibilityBufferStep.pixelWrites", 0);
            context.Counters.Set("DefaultBuildFaceIdVisibilityBufferStep.visibleFaces", 0);
            return;
        }

        var scaleX = buffer.Width / (float)NumericMath.AtLeast(context.Width, 1);
        var scaleY = buffer.Height / (float)NumericMath.AtLeast(context.Height, 1);
        var tileSize = RasterMath.AtLeastPixels(VisibilityTileSize, 8);
        var tilesPerRow = RasterMath.TilesPerAxis(buffer.Width, tileSize);
        var tileRows = RasterMath.TilesPerAxis(buffer.Height, tileSize);
        var tileCount = tilesPerRow * tileRows;
        EnsureTileLayout(buffer.Width, buffer.Height, tileSize);
        var rangeCount = DeterministicParallel.GetRangeCount(triangleCount, context.WorkerCount, 64);

        if (context.WorkerCount <= 1 || triangleCount < 256 || tileCount <= 1 || rangeCount <= 1)
        {
            long pixelTests = 0;
            long pixelWrites = 0;
            RasterizeSequential(
                buffer,
                context.Graph.Triangles,
                context.Graph.Vertices,
                scaleX,
                scaleY,
                ref pixelTests,
                ref pixelWrites);
            buffer.MarkVisibleFaces(clear: false);
            context.Counters.Set("DefaultBuildFaceIdVisibilityBufferStep.tileCount", tileCount);
            context.Counters.Set("DefaultBuildFaceIdVisibilityBufferStep.rangeCount", 1);
            context.Counters.Set("DefaultBuildFaceIdVisibilityBufferStep.totalTileRefs", 0);
            context.Counters.Set("DefaultBuildFaceIdVisibilityBufferStep.validRasterInfoCount", triangleCount);
            context.Counters.Set("DefaultBuildFaceIdVisibilityBufferStep.maxRefsPerTile", 0);
            context.Counters.Set("DefaultBuildFaceIdVisibilityBufferStep.pixelTests", pixelTests);
            context.Counters.Set("DefaultBuildFaceIdVisibilityBufferStep.pixelWrites", pixelWrites);
            context.Counters.Set("DefaultBuildFaceIdVisibilityBufferStep.visibleFaces", CountVisibleFaces(buffer.FaceVisible));
            return;
        }

        EnsureScratchCapacity(triangleCount, rangeCount, tileCount);
        var triangles = context.Graph.Triangles;
        var vertices = context.Graph.Vertices;

        DeterministicParallel.ForRanges(
            0,
            triangleCount,
            context.WorkerCount,
            context.CancellationToken,
            (startInclusive, endExclusive, _, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var triangleSpan = CollectionsMarshal.AsSpan(triangles);
                var vertexSpan = CollectionsMarshal.AsSpan(vertices);
                for (var triangleIndex = startInclusive; triangleIndex < endExclusive; triangleIndex++)
                {
                    if ((triangleIndex & 0x3FF) == 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    ref readonly var triangle = ref triangleSpan[triangleIndex];
                    _triangleInfos[triangleIndex] = BuildTriangleRasterInfo(
                        in triangle,
                        triangleIndex,
                        vertexSpan,
                        scaleX,
                        scaleY,
                        buffer.Width,
                        buffer.Height,
                        tileSize,
                        tilesPerRow);
                }
            },
            minItemsPerRange: 64);

        var validRasterInfoCount = 0;
        for (var triangleIndex = 0; triangleIndex < triangleCount; triangleIndex++)
        {
            if (_triangleInfos[triangleIndex].IsValid)
            {
                validRasterInfoCount++;
            }
        }

        Array.Clear(_rangeTileCounts, 0, rangeCount * tileCount);
        DeterministicParallel.ForRanges(
            0,
            triangleCount,
            context.WorkerCount,
            context.CancellationToken,
            (startInclusive, endExclusive, rangeIndex, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var rangeBase = rangeIndex * tileCount;
                for (var triangleIndex = startInclusive; triangleIndex < endExclusive; triangleIndex++)
                {
                    if ((triangleIndex & 0x3FF) == 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    ref readonly var info = ref _triangleInfos[triangleIndex];
                    if (!info.IsValid)
                    {
                        continue;
                    }

                    for (var tileY = info.MinTileY; tileY <= info.MaxTileY; tileY++)
                    {
                        var tileRowBase = tileY * tilesPerRow;
                        for (var tileX = info.MinTileX; tileX <= info.MaxTileX; tileX++)
                        {
                            _rangeTileCounts[rangeBase + tileRowBase + tileX]++;
                        }
                    }
                }
            },
            minItemsPerRange: 64);

        Array.Clear(_tileCounts, 0, tileCount);
        for (var tileIndex = 0; tileIndex < tileCount; tileIndex++)
        {
            var total = 0;
            for (var rangeIndex = 0; rangeIndex < rangeCount; rangeIndex++)
            {
                total += _rangeTileCounts[rangeIndex * tileCount + tileIndex];
            }

            _tileCounts[tileIndex] = total;
        }

        var maxRefsPerTile = 0;
        for (var tileIndex = 0; tileIndex < tileCount; tileIndex++)
        {
            maxRefsPerTile = NumericMath.AtLeast(maxRefsPerTile, _tileCounts[tileIndex]);
        }

        var totalRefs = PrefixSums.ExclusiveFromCounts(_tileCounts.AsSpan(0, tileCount), _tileOffsets.AsSpan(0, tileCount));
        EnsureTileRefCapacity(totalRefs);

        for (var tileIndex = 0; tileIndex < tileCount; tileIndex++)
        {
            var cursor = _tileOffsets[tileIndex];
            for (var rangeIndex = 0; rangeIndex < rangeCount; rangeIndex++)
            {
                var count = _rangeTileCounts[rangeIndex * tileCount + tileIndex];
                _rangeTileOffsets[rangeIndex * tileCount + tileIndex] = cursor;
                cursor += count;
            }
        }

        Array.Copy(_rangeTileOffsets, _tileWriteCursors, _rangeTileOffsets.Length);
        Array.Clear(_tilePixelTests, 0, tileCount);
        Array.Clear(_tilePixelWrites, 0, tileCount);
        DeterministicParallel.ForRanges(
            0,
            triangleCount,
            context.WorkerCount,
            context.CancellationToken,
            (startInclusive, endExclusive, rangeIndex, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var rangeBase = rangeIndex * tileCount;
                for (var triangleIndex = startInclusive; triangleIndex < endExclusive; triangleIndex++)
                {
                    if ((triangleIndex & 0x3FF) == 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    ref readonly var info = ref _triangleInfos[triangleIndex];
                    if (!info.IsValid)
                    {
                        continue;
                    }

                    for (var tileY = info.MinTileY; tileY <= info.MaxTileY; tileY++)
                    {
                        var tileRowBase = tileY * tilesPerRow;
                        for (var tileX = info.MinTileX; tileX <= info.MaxTileX; tileX++)
                        {
                            var tileIndex = tileRowBase + tileX;
                            var writeIndex = _tileWriteCursors[rangeBase + tileIndex]++;
                            _tileTriangleIndices[writeIndex] = triangleIndex;
                        }
                    }
                }
            },
            minItemsPerRange: 64);

        DeterministicParallel.ForRanges(
            0,
            tileCount,
            context.WorkerCount,
            context.CancellationToken,
            (startInclusive, endExclusive, _, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                for (var tileIndex = startInclusive; tileIndex < endExclusive; tileIndex++)
                {
                    if ((tileIndex & 0x3FF) == 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    long localPixelTests = 0;
                    long localPixelWrites = 0;
                    RasterizeTile(
                        buffer,
                        tileIndex,
                        tileSize,
                        tilesPerRow,
                        totalRefs,
                        _tileOffsets[tileIndex],
                        _tileCounts[tileIndex],
                        _tileTriangleIndices,
                        _triangleInfos,
                        ref localPixelTests,
                        ref localPixelWrites);
                    _tilePixelTests[tileIndex] = localPixelTests;
                    _tilePixelWrites[tileIndex] = localPixelWrites;
                }
            },
            minItemsPerRange: 1);

        buffer.MarkVisibleFaces(clear: false);
        long totalPixelTests = 0;
        long totalPixelWrites = 0;
        for (var tileIndex = 0; tileIndex < tileCount; tileIndex++)
        {
            totalPixelTests += _tilePixelTests[tileIndex];
            totalPixelWrites += _tilePixelWrites[tileIndex];
        }

        context.Counters.Set("DefaultBuildFaceIdVisibilityBufferStep.tileCount", tileCount);
        context.Counters.Set("DefaultBuildFaceIdVisibilityBufferStep.rangeCount", rangeCount);
        context.Counters.Set("DefaultBuildFaceIdVisibilityBufferStep.totalTileRefs", totalRefs);
        context.Counters.Set("DefaultBuildFaceIdVisibilityBufferStep.validRasterInfoCount", validRasterInfoCount);
        context.Counters.Set("DefaultBuildFaceIdVisibilityBufferStep.maxRefsPerTile", maxRefsPerTile);
        context.Counters.Set("DefaultBuildFaceIdVisibilityBufferStep.pixelTests", totalPixelTests);
        context.Counters.Set("DefaultBuildFaceIdVisibilityBufferStep.pixelWrites", totalPixelWrites);
        context.Counters.Set("DefaultBuildFaceIdVisibilityBufferStep.visibleFaces", CountVisibleFaces(buffer.FaceVisible));
        context.Counters.Set("DefaultBuildFaceIdVisibilityBufferStep.tileLayoutCacheWidth", _lastWidth);
        context.Counters.Set("DefaultBuildFaceIdVisibilityBufferStep.tileLayoutCacheHeight", _lastHeight);
        context.Counters.Set("DefaultBuildFaceIdVisibilityBufferStep.tileLayoutCacheTiles", _lastTileCount);
    }

    private static int CountVisibleFaces(bool[] faceVisible)
    {
        var count = 0;
        for (var i = 0; i < faceVisible.Length; i++)
        {
            if (faceVisible[i])
            {
                count++;
            }
        }

        return count;
    }

    private DefaultFaceIdVisibilityBuffer RentBuffer(int width, int height, int faceCount)
    {
        if (_buffer is null ||
            _buffer.Width != width ||
            _buffer.Height != height ||
            _buffer.FaceCapacity < faceCount)
        {
            _buffer = new DefaultFaceIdVisibilityBuffer(width, height, faceCount);
        }

        return _buffer;
    }

    private void EnsureTileLayout(int width, int height, int tileSize)
    {
        var tileCountX = RasterMath.TilesPerAxis(width, tileSize);
        var tileCountY = RasterMath.TilesPerAxis(height, tileSize);
        var tileCount = tileCountX * tileCountY;

        if (_lastWidth == width &&
            _lastHeight == height &&
            _lastTileCount == tileCount)
        {
            return;
        }

        _lastWidth = width;
        _lastHeight = height;
        _lastTileCountX = tileCountX;
        _lastTileCountY = tileCountY;
        _lastTileCount = tileCount;
        EnsureTileScratchCapacity(tileCount);
    }

    private void EnsureTileScratchCapacity(int tileCount)
    {
        if (_tileCounts.Length < tileCount)
        {
            _tileCounts = new int[tileCount];
            _tileOffsets = new int[tileCount];
        }

        if (_tilePixelTests.Length < tileCount)
        {
            _tilePixelTests = new long[tileCount];
            _tilePixelWrites = new long[tileCount];
        }
    }

    private void EnsureScratchCapacity(int triangleCount, int rangeCount, int tileCount)
    {
        if (_triangleInfos.Length < triangleCount)
        {
            _triangleInfos = new TriangleRasterInfo[triangleCount];
        }

        var rangeTileLength = rangeCount * tileCount;
        if (_rangeTileCounts.Length < rangeTileLength)
        {
            _rangeTileCounts = new int[rangeTileLength];
            _rangeTileOffsets = new int[rangeTileLength];
        }

        if (_tileCounts.Length < tileCount)
        {
            _tileCounts = new int[tileCount];
            _tileOffsets = new int[tileCount];
        }

        if (_tileWriteCursors.Length < rangeTileLength)
        {
            _tileWriteCursors = new int[rangeTileLength];
        }

        if (_tilePixelTests.Length < tileCount)
        {
            _tilePixelTests = new long[tileCount];
            _tilePixelWrites = new long[tileCount];
        }
    }

    private void EnsureTileRefCapacity(int totalRefs)
    {
        if (_tileTriangleIndices.Length < totalRefs)
        {
            _tileTriangleIndices = new int[totalRefs];
        }
    }

    private static TriangleRasterInfo BuildTriangleRasterInfo(
        in ProjectedTriangle triangle,
        int triangleIndex,
        ReadOnlySpan<ProjectedVertex> vertices,
        float scaleX,
        float scaleY,
        int bufferWidth,
        int bufferHeight,
        int tileSize,
        int tilesPerRow)
    {
        var a = vertices[triangle.A];
        var b = vertices[triangle.B];
        var c = vertices[triangle.C];

        if (ClipSpaceMath.TriangleOutsideCanonicalClip(a.Ndc, b.Ndc, c.Ndc))
        {
            return default;
        }

        var ax = a.Position.X * scaleX;
        var ay = a.Position.Y * scaleY;
        var bx = b.Position.X * scaleX;
        var by = b.Position.Y * scaleY;
        var cx = c.Position.X * scaleX;
        var cy = c.Position.Y * scaleY;
        var area = DefaultFaceIdVisibilityBuffer.EdgeFunction(ax, ay, bx, by, cx, cy);
        if (NumericMath.Abs(area) < 1e-7f)
        {
            return default;
        }

        var (minX, maxX, minY, maxY) = RasterMath.TrianglePixelBounds(ax, ay, bx, by, cx, cy, bufferWidth, bufferHeight);
        if (minX > maxX || minY > maxY)
        {
            return default;
        }

        var invArea = 1f / area;
        var stepX0 = cy - by;
        var stepY0 = -(cx - bx);
        var stepX1 = ay - cy;
        var stepY1 = -(ax - cx);
        var stepX2 = by - ay;
        var stepY2 = -(bx - ax);

        var xTileRange = RasterMath.TileRangeFromPixelRange(minX, maxX, tileSize, tilesPerRow);
        var tileRows = RasterMath.TilesPerAxis(bufferHeight, tileSize);
        var yTileRange = RasterMath.TileRangeFromPixelRange(minY, maxY, tileSize, tileRows);

        return new TriangleRasterInfo(
            true,
            triangleIndex,
            ax,
            ay,
            bx,
            by,
            cx,
            cy,
            area,
            invArea,
            stepX0,
            stepY0,
            stepX1,
            stepY1,
            stepX2,
            stepY2,
            a.Depth01 * invArea,
            b.Depth01 * invArea,
            c.Depth01 * invArea,
            area >= 0f,
            minX,
            maxX,
            minY,
            maxY,
            xTileRange.MinTile,
            xTileRange.MaxTile,
            yTileRange.MinTile,
            yTileRange.MaxTile,
            a.Depth01,
            b.Depth01,
            c.Depth01);
    }

    private static void RasterizeSequential(
        DefaultFaceIdVisibilityBuffer buffer,
        List<ProjectedTriangle> triangles,
        List<ProjectedVertex> vertices,
        float scaleX,
        float scaleY,
        ref long pixelTests,
        ref long pixelWrites)
    {
        var triangleSpan = CollectionsMarshal.AsSpan(triangles);
        var vertexSpan = CollectionsMarshal.AsSpan(vertices);
        for (var triangleIndex = 0; triangleIndex < triangleSpan.Length; triangleIndex++)
        {
            ref readonly var triangle = ref triangleSpan[triangleIndex];
            var a = vertexSpan[triangle.A];
            var b = vertexSpan[triangle.B];
            var c = vertexSpan[triangle.C];

            if (ClipSpaceMath.TriangleOutsideCanonicalClip(a.Ndc, b.Ndc, c.Ndc))
            {
                continue;
            }

            Rasterize(
                buffer,
                triangleIndex,
                scaleX,
                scaleY,
                a,
                b,
                c,
                ref pixelTests,
                ref pixelWrites);
        }
    }

    private static void RasterizeTile(
        DefaultFaceIdVisibilityBuffer buffer,
        int tileIndex,
        int tileSize,
        int tilesPerRow,
        int totalRefs,
        int tileOffset,
        int tileCount,
        int[] tileTriangleIndices,
        TriangleRasterInfo[] triangleInfos,
        ref long pixelTests,
        ref long pixelWrites)
    {
        if (tileCount <= 0)
        {
            return;
        }

        var tileX = tileIndex % tilesPerRow;
        var tileY = tileIndex / tilesPerRow;
        var tileMinX = tileX * tileSize;
        var tileMinY = tileY * tileSize;
        var tileMaxX = NumericMath.AtMost(buffer.Width - 1, tileMinX + tileSize - 1);
        var tileMaxY = NumericMath.AtMost(buffer.Height - 1, tileMinY + tileSize - 1);
        var depthBuffer = buffer.Depth;
        var faceBuffer = buffer.FaceId;

        for (var refIndex = tileOffset; refIndex < tileOffset + tileCount && refIndex < totalRefs; refIndex++)
        {
            var triangleIndex = tileTriangleIndices[refIndex];
            ref readonly var triangle = ref triangleInfos[triangleIndex];
            var minX = NumericMath.AtLeast(tileMinX, triangle.MinX);
            var maxX = NumericMath.AtMost(tileMaxX, triangle.MaxX);
            var minY = NumericMath.AtLeast(tileMinY, triangle.MinY);
            var maxY = NumericMath.AtMost(tileMaxY, triangle.MaxY);
            if (minX > maxX || minY > maxY)
            {
                continue;
            }

            RasterizeClipped(
                buffer,
                triangleIndex,
                minX,
                maxX,
                minY,
                maxY,
                triangle,
                depthBuffer,
                faceBuffer,
                ref pixelTests,
                ref pixelWrites);
        }
    }

    private static unsafe void RasterizeClipped(
        DefaultFaceIdVisibilityBuffer buffer,
        int triangleIndex,
        int minX,
        int maxX,
        int minY,
        int maxY,
        in TriangleRasterInfo triangle,
        float[] depthBuffer,
        int[] faceBuffer,
        ref long pixelTests,
        ref long pixelWrites)
    {
        var rowStartX = minX + 0.5f;
        var rowStartY = minY + 0.5f;
        var rowW0 = DefaultFaceIdVisibilityBuffer.EdgeFunction(triangle.Bx, triangle.By, triangle.Cx, triangle.Cy, rowStartX, rowStartY);
        var rowW1 = DefaultFaceIdVisibilityBuffer.EdgeFunction(triangle.Cx, triangle.Cy, triangle.Ax, triangle.Ay, rowStartX, rowStartY);
        var rowW2 = DefaultFaceIdVisibilityBuffer.EdgeFunction(triangle.Ax, triangle.Ay, triangle.Bx, triangle.By, rowStartX, rowStartY);
        var width = buffer.Width;

        fixed (float* depthBufferPtr = depthBuffer)
        fixed (int* faceBufferPtr = faceBuffer)
        {
            if (triangle.PositiveArea)
            {
                for (var y = minY; y <= maxY; y++)
                {
                    var w0 = rowW0;
                    var w1 = rowW1;
                    var w2 = rowW2;
                    var rowIndex = y * width + minX;
                    var depthCursor = depthBufferPtr + rowIndex;
                    var faceCursor = faceBufferPtr + rowIndex;

                    for (var x = minX; x <= maxX; x++)
                    {
                        pixelTests++;
                        if (w0 >= -1e-5f && w1 >= -1e-5f && w2 >= -1e-5f)
                        {
                            var depth = w0 * triangle.DepthScale0 + w1 * triangle.DepthScale1 + w2 * triangle.DepthScale2;
                            var currentDepth = *depthCursor;
                            var currentFace = *faceCursor;

                            if (RasterDepthMath.ShouldWriteDepth(depth, currentDepth, triangleIndex, currentFace))
                            {
                                *depthCursor = depth;
                                *faceCursor = triangleIndex;
                                pixelWrites++;
                            }
                        }

                        w0 += triangle.StepX0;
                        w1 += triangle.StepX1;
                        w2 += triangle.StepX2;
                        depthCursor++;
                        faceCursor++;
                    }

                    rowW0 += triangle.StepY0;
                    rowW1 += triangle.StepY1;
                    rowW2 += triangle.StepY2;
                }
            }
            else
            {
                for (var y = minY; y <= maxY; y++)
                {
                    var w0 = rowW0;
                    var w1 = rowW1;
                    var w2 = rowW2;
                    var rowIndex = y * width + minX;
                    var depthCursor = depthBufferPtr + rowIndex;
                    var faceCursor = faceBufferPtr + rowIndex;

                    for (var x = minX; x <= maxX; x++)
                    {
                        pixelTests++;
                        if (w0 <= 1e-5f && w1 <= 1e-5f && w2 <= 1e-5f)
                        {
                            var depth = w0 * triangle.DepthScale0 + w1 * triangle.DepthScale1 + w2 * triangle.DepthScale2;
                            var currentDepth = *depthCursor;
                            var currentFace = *faceCursor;

                            if (RasterDepthMath.ShouldWriteDepth(depth, currentDepth, triangleIndex, currentFace))
                            {
                                *depthCursor = depth;
                                *faceCursor = triangleIndex;
                                pixelWrites++;
                            }
                        }

                        w0 += triangle.StepX0;
                        w1 += triangle.StepX1;
                        w2 += triangle.StepX2;
                        depthCursor++;
                        faceCursor++;
                    }

                    rowW0 += triangle.StepY0;
                    rowW1 += triangle.StepY1;
                    rowW2 += triangle.StepY2;
                }
            }
        }
    }

    private static void Rasterize(
        DefaultFaceIdVisibilityBuffer buffer,
        int triangleIndex,
        float scaleX,
        float scaleY,
        in ProjectedVertex a,
        in ProjectedVertex b,
        in ProjectedVertex c,
        ref long pixelTests,
        ref long pixelWrites)
    {
        var ax = a.Position.X * scaleX;
        var ay = a.Position.Y * scaleY;
        var bx = b.Position.X * scaleX;
        var by = b.Position.Y * scaleY;
        var cx = c.Position.X * scaleX;
        var cy = c.Position.Y * scaleY;
        var area = DefaultFaceIdVisibilityBuffer.EdgeFunction(ax, ay, bx, by, cx, cy);
        if (NumericMath.Abs(area) < 1e-7f)
        {
            return;
        }

        var (minX, maxX, minY, maxY) = RasterMath.TrianglePixelBounds(ax, ay, bx, by, cx, cy, buffer.Width, buffer.Height);

        var stepX0 = cy - by;
        var stepY0 = -(cx - bx);
        var stepX1 = ay - cy;
        var stepY1 = -(ax - cx);
        var stepX2 = by - ay;
        var stepY2 = -(bx - ax);
        var rowStartX = minX + 0.5f;
        var rowStartY = minY + 0.5f;
        var rowW0 = DefaultFaceIdVisibilityBuffer.EdgeFunction(bx, by, cx, cy, rowStartX, rowStartY);
        var rowW1 = DefaultFaceIdVisibilityBuffer.EdgeFunction(cx, cy, ax, ay, rowStartX, rowStartY);
        var rowW2 = DefaultFaceIdVisibilityBuffer.EdgeFunction(ax, ay, bx, by, rowStartX, rowStartY);
        var invArea = 1f / area;
        var depthScale0 = a.Depth01 * invArea;
        var depthScale1 = b.Depth01 * invArea;
        var depthScale2 = c.Depth01 * invArea;
        var width = buffer.Width;
        var depthBuffer = buffer.Depth;
        var faceBuffer = buffer.FaceId;
        if (area >= 0f)
        {
            for (var y = minY; y <= maxY; y++)
            {
                var w0 = rowW0;
                var w1 = rowW1;
                var w2 = rowW2;
                var rowIndex = y * width + minX;

                for (var x = minX; x <= maxX; x++)
                {
                    pixelTests++;
                    if (w0 >= -1e-5f && w1 >= -1e-5f && w2 >= -1e-5f)
                    {
                        var depth = w0 * depthScale0 + w1 * depthScale1 + w2 * depthScale2;

                        if (RasterDepthMath.ShouldWriteDepth(depth, depthBuffer[rowIndex], triangleIndex, faceBuffer[rowIndex]))
                        {
                            depthBuffer[rowIndex] = depth;
                            faceBuffer[rowIndex] = triangleIndex;
                            pixelWrites++;
                        }
                    }

                    w0 += stepX0;
                    w1 += stepX1;
                    w2 += stepX2;
                    rowIndex++;
                }

                rowW0 += stepY0;
                rowW1 += stepY1;
                rowW2 += stepY2;
            }
        }
        else
        {
            for (var y = minY; y <= maxY; y++)
            {
                var w0 = rowW0;
                var w1 = rowW1;
                var w2 = rowW2;
                var rowIndex = y * width + minX;

                for (var x = minX; x <= maxX; x++)
                {
                    pixelTests++;
                    if (w0 <= 1e-5f && w1 <= 1e-5f && w2 <= 1e-5f)
                    {
                        var depth = w0 * depthScale0 + w1 * depthScale1 + w2 * depthScale2;

                        if (RasterDepthMath.ShouldWriteDepth(depth, depthBuffer[rowIndex], triangleIndex, faceBuffer[rowIndex]))
                        {
                            depthBuffer[rowIndex] = depth;
                            faceBuffer[rowIndex] = triangleIndex;
                            pixelWrites++;
                        }
                    }

                    w0 += stepX0;
                    w1 += stepX1;
                    w2 += stepX2;
                    rowIndex++;
                }

                rowW0 += stepY0;
                rowW1 += stepY1;
                rowW2 += stepY2;
            }
        }
    }

    private readonly record struct TriangleRasterInfo(
        bool IsValid,
        int TriangleIndex,
        float Ax,
        float Ay,
        float Bx,
        float By,
        float Cx,
        float Cy,
        float Area,
        float InvArea,
        float StepX0,
        float StepY0,
        float StepX1,
        float StepY1,
        float StepX2,
        float StepY2,
        float DepthScale0,
        float DepthScale1,
        float DepthScale2,
        bool PositiveArea,
        int MinX,
        int MaxX,
        int MinY,
        int MaxY,
        int MinTileX,
        int MaxTileX,
        int MinTileY,
        int MaxTileY,
        float Depth0,
        float Depth1,
        float Depth2);
}
