namespace STFU.Strokes;

public readonly record struct Point2D(float X, float Y);

public readonly record struct Stroke2D(
    Point2D Start,
    Point2D End,
    float Thickness);
