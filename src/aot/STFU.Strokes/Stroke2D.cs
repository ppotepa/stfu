namespace STFU.Strokes;

public readonly record struct Point2D(float X, float Y);

public readonly record struct StrokeColor(byte R, byte G, byte B)
{
    public static StrokeColor Black { get; } = new(20, 20, 20);
}

public readonly record struct StrokeStyle2D(
    float Thickness,
    float Opacity,
    StrokeColor Color)
{
    public static StrokeStyle2D Default { get; } = new(1.0f, 1.0f, StrokeColor.Black);
}

public readonly record struct Stroke2D(
    Point2D Start,
    Point2D End,
    float Thickness);

public readonly record struct StrokePoint2D(
    Point2D Position,
    float Thickness,
    float Opacity,
    float Pressure = 1f)
{
    public static StrokePoint2D FromPoint(Point2D position, StrokeStyle2D style, float pressure = 1f)
    {
        return new StrokePoint2D(position, style.Thickness, style.Opacity, pressure);
    }
}

public readonly record struct StrokeMetadata(
    int StableId,
    string? Layer,
    string? SourceKind,
    string? Intent = null,
    int? SourceFeatureId = null,
    int? SourceSegmentId = null,
    string? Visibility = null,
    string? StyleId = null,
    string? Variant = null,
    int LayerOrder = 0,
    int? EntityId = null);

public sealed class StrokePath2D
{
    private readonly IReadOnlyList<Point2D>? _points;
    private readonly Point2D _segmentStart;
    private readonly Point2D _segmentEnd;
    private readonly bool _isSegment;
    private Point2D[]? _materializedSegmentPoints;

    public StrokePath2D(
        IReadOnlyList<Point2D> points,
        StrokeStyle2D style,
        IReadOnlyList<StrokePoint2D>? richPoints = null,
        StrokeMetadata? metadata = null)
    {
        _points = points ?? [];
        Style = style;
        RichPoints = richPoints;
        Metadata = metadata;
    }

    public StrokePath2D(
        Point2D start,
        Point2D end,
        StrokeStyle2D style,
        IReadOnlyList<StrokePoint2D>? richPoints = null,
        StrokeMetadata? metadata = null)
    {
        _isSegment = true;
        _segmentStart = start;
        _segmentEnd = end;
        Style = style;
        RichPoints = richPoints;
        Metadata = metadata;
    }

    public IReadOnlyList<Point2D> Points => _isSegment
        ? _materializedSegmentPoints ??= [_segmentStart, _segmentEnd]
        : _points!;

    public StrokeStyle2D Style { get; }

    public IReadOnlyList<StrokePoint2D>? RichPoints { get; }

    public StrokeMetadata? Metadata { get; }

    public static StrokePath2D Line(Point2D start, Point2D end, StrokeStyle2D style)
    {
        return new StrokePath2D(start, end, style);
    }

    public bool TryGetSegment(out Point2D start, out Point2D end)
    {
        if (_isSegment)
        {
            start = _segmentStart;
            end = _segmentEnd;
            return true;
        }

        start = default;
        end = default;
        return false;
    }
}
