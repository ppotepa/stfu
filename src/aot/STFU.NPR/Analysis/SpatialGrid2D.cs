namespace STFU.NPR.Analysis;

public sealed class SpatialGrid2D<T>
{
    private readonly ScreenTileGrid<T> _inner;

    public int TileSize => _inner.TileSize;

    public SpatialGrid2D(int tileSize)
    {
        _inner = new ScreenTileGrid<T>(tileSize);
    }

    public void Add(float x, float y, T item)
    {
        _inner.Add(x, y, item);
    }

    public IEnumerable<KeyValuePair<(int X, int Y), List<T>>> EnumerateTiles()
    {
        return _inner.EnumerateTiles();
    }
}
