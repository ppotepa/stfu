using STFU.Common.Math;

namespace STFU.Rendering.Cpu.Rasterization;

public readonly record struct CpuTileLayout(
    int Width,
    int Height,
    int TileSize,
    int TileCountX,
    int TileCountY,
    int TileCount)
{
    public static CpuTileLayout Empty { get; } = new(0, 0, 0, 0, 0, 0);

    public static CpuTileLayout Create(int width, int height, int tileSize)
    {
        tileSize = RasterMath.ClampTileSize(tileSize);
        var tileCountX = width <= 0 ? 0 : (width + tileSize - 1) / tileSize;
        var tileCountY = height <= 0 ? 0 : (height + tileSize - 1) / tileSize;
        var tileCount = tileCountX * tileCountY;
        return new CpuTileLayout(width, height, tileSize, tileCountX, tileCountY, tileCount);
    }
}
