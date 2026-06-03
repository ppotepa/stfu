using STFU.Strokes;

namespace STFU.NPR.Graph;

public readonly record struct FeatureLine(
    int StableId,
    NprStrokeIntent Intent,
    Point2D Start,
    Point2D End,
    float Depth,
    float Shade,
    float Importance);
