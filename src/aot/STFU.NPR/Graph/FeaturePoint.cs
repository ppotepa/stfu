using STFU.Strokes;

namespace STFU.NPR.Graph;

public readonly record struct FeaturePoint(
    Point2D ScreenPosition,
    float Depth);
