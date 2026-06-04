namespace STFU.Rendering.Cpu.Rasterization;

public static class CpuTileScheduler
{
    public static IReadOnlyList<CpuTile> BuildTiles(int width, int height, int tileSize)
    {
        tileSize = Math.Clamp(tileSize, 8, 256);
        var tiles = new List<CpuTile>();
        for (var y = 0; y < height; y += tileSize)
        {
            for (var x = 0; x < width; x += tileSize)
            {
                tiles.Add(new CpuTile(
                    x,
                    y,
                    Math.Min(tileSize, width - x),
                    Math.Min(tileSize, height - y)));
            }
        }

        return tiles;
    }

    public static void ForEachTile(
        int width,
        int height,
        int tileSize,
        int workerCount,
        bool parallel,
        Action<CpuTile> action)
    {
        var tiles = BuildTiles(width, height, tileSize);
        if (!parallel || workerCount <= 1 || tiles.Count <= 1)
        {
            foreach (var tile in tiles)
            {
                action(tile);
            }

            return;
        }

        Parallel.ForEach(
            tiles,
            new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, workerCount) },
            action);
    }
}
