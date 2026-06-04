namespace STFU.NPR.Analysis;

public sealed class ScreenTileGrid<T>
{
    private readonly Dictionary<(int X, int Y), List<T>> _tiles = new();

    public int TileSize { get; }

    public IReadOnlyDictionary<(int X, int Y), List<T>> Tiles => _tiles;

    public ScreenTileGrid(int tileSize)
    {
        TileSize = Math.Max(1, tileSize);
    }

    public (int X, int Y) GetTileKey(float x, float y)
    {
        return (
            (int)MathF.Floor(x / TileSize),
            (int)MathF.Floor(y / TileSize));
    }

    public void Add(float x, float y, T item)
    {
        var key = GetTileKey(x, y);
        if (!_tiles.TryGetValue(key, out var bucket))
        {
            bucket = [];
            _tiles.Add(key, bucket);
        }

        bucket.Add(item);
    }

    public IEnumerable<KeyValuePair<(int X, int Y), List<T>>> EnumerateTiles()
    {
        return _tiles;
    }
}
