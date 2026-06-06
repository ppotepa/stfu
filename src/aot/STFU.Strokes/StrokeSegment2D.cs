namespace STFU.Strokes;

public readonly record struct StrokeSegment2D(
    Point2D Start,
    Point2D End,
    StrokeStyle2D Style,
    StrokeMetadata? Metadata = null,
    StrokePoint2D? RichStart = null,
    StrokePoint2D? RichEnd = null);
