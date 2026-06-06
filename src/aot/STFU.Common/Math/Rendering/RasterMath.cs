namespace STFU.Common.Math;

public static class RasterMath
{
    public const int MinTileSize = 8;

    public const int MaxTileSize = 256;

    public static int ClampTileSize(int tileSize)
    {
        return global::System.Math.Clamp(tileSize, MinTileSize, MaxTileSize);
    }

    public static int AtLeastPixels(int value, int minimum)
    {
        return global::System.Math.Max(minimum, value);
    }

    public static int PixelCount(int width, int height)
    {
        return checked(AtLeastPixels(width, 1) * AtLeastPixels(height, 1));
    }

    public static (int X, int Y) TileKey(float x, float y, int tileSize)
    {
        var safeTileSize = AtLeastPixels(tileSize, 1);
        return (
            (int)MathF.Floor(x / safeTileSize),
            (int)MathF.Floor(y / safeTileSize));
    }

    public static int TilesPerAxis(int pixels, int tileSize)
    {
        var safeTileSize = AtLeastPixels(tileSize, 1);
        return global::System.Math.Max(1, (pixels + safeTileSize - 1) / safeTileSize);
    }

    public static int TileIndexFromCoordinate(float coordinate, int tileSize, int tileCount)
    {
        var safeTileSize = AtLeastPixels(tileSize, 1);
        return global::System.Math.Clamp(
            (int)MathF.Floor(coordinate / safeTileSize),
            0,
            global::System.Math.Max(0, tileCount - 1));
    }

    public static int ToBufferCoordinate(float screenCoordinate, int viewportPixels, int bufferPixels)
    {
        return global::System.Math.Clamp(
            (int)MathF.Floor((screenCoordinate / global::System.Math.Max(1, viewportPixels)) * bufferPixels),
            0,
            global::System.Math.Max(0, bufferPixels - 1));
    }

    public static (int Start, int End) ClampPixelRange(float min, float max, int lowerInclusive, int upperExclusive)
    {
        var start = global::System.Math.Max(lowerInclusive, (int)MathF.Floor(min));
        var end = global::System.Math.Min(upperExclusive - 1, (int)MathF.Ceiling(max));
        return (start, end);
    }

    public static (int MinX, int MaxX, int MinY, int MaxY) TrianglePixelBounds(
        float ax,
        float ay,
        float bx,
        float by,
        float cx,
        float cy,
        int width,
        int height,
        float padding = 1f)
    {
        var minX = global::System.Math.Max(0, (int)MathF.Floor(MathF.Min(ax, MathF.Min(bx, cx)) - padding));
        var maxX = global::System.Math.Min(width - 1, (int)MathF.Ceiling(MathF.Max(ax, MathF.Max(bx, cx)) + padding));
        var minY = global::System.Math.Max(0, (int)MathF.Floor(MathF.Min(ay, MathF.Min(by, cy)) - padding));
        var maxY = global::System.Math.Min(height - 1, (int)MathF.Ceiling(MathF.Max(ay, MathF.Max(by, cy)) + padding));
        return (minX, maxX, minY, maxY);
    }

    public static (int MinTile, int MaxTile) TileRangeFromPixelRange(int minPixel, int maxPixel, int tileSize, int tileCount)
    {
        var safeTileSize = AtLeastPixels(tileSize, 1);
        return (
            global::System.Math.Max(0, minPixel / safeTileSize),
            global::System.Math.Min(global::System.Math.Max(0, tileCount - 1), maxPixel / safeTileSize));
    }

    public static (int X, int Y, int Width, int Height) TileBounds(
        int tileIndex,
        int tilesPerRow,
        int tileSize,
        int targetWidth,
        int targetHeight)
    {
        var safeTilesPerRow = AtLeastPixels(tilesPerRow, 1);
        var safeTileSize = AtLeastPixels(tileSize, 1);
        var tileX = tileIndex % safeTilesPerRow;
        var tileY = tileIndex / safeTilesPerRow;
        var x = tileX * safeTileSize;
        var y = tileY * safeTileSize;
        return (
            x,
            y,
            global::System.Math.Min(safeTileSize, targetWidth - x),
            global::System.Math.Min(safeTileSize, targetHeight - y));
    }

    public static float EdgeFunction(float ax, float ay, float bx, float by, float px, float py)
    {
        return (px - ax) * (by - ay) - (py - ay) * (bx - ax);
    }
}
