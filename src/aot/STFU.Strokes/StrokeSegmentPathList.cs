namespace STFU.Strokes;

public sealed class StrokeSegmentPathList : IReadOnlyList<StrokePath2D>
{
    private readonly IReadOnlyList<StrokeSegment2D> _segments;
    private StrokePath2D[]? _paths;

    public StrokeSegmentPathList(IReadOnlyList<StrokeSegment2D> segments)
    {
        _segments = segments;
    }

    public int Count => _segments.Count;

    public StrokePath2D this[int index]
    {
        get
        {
            var paths = _paths;
            if (paths is null)
            {
                paths = new StrokePath2D[_segments.Count];
                _paths = paths;
            }

            var path = paths[index];
            if (path is not null)
            {
                return path;
            }

            var segment = _segments[index];
            path = segment.RichStart is StrokePoint2D richStart && segment.RichEnd is StrokePoint2D richEnd
                ? new StrokePath2D(
                    segment.Start,
                    segment.End,
                    segment.Style,
                    [richStart, richEnd],
                    segment.Metadata)
                : new StrokePath2D(
                    segment.Start,
                    segment.End,
                    segment.Style,
                    null,
                    segment.Metadata);

            paths[index] = path;
            return path;
        }
    }

    public IEnumerator<StrokePath2D> GetEnumerator()
    {
        for (var i = 0; i < _segments.Count; i++)
        {
            yield return this[i];
        }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
