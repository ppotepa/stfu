using STFU.Strokes;

namespace STFU.NPR.Debug;

public sealed record NprDebugLine(
    DebugOverlayKind Kind,
    Point2D Start,
    Point2D End,
    string Label,
    float Depth,
    bool IsPrimary,
    int SourceId = 0,
    float Value = 0f);
