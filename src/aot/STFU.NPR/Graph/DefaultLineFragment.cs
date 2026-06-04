using STFU.Strokes;

namespace STFU.NPR.Graph;

public sealed record DefaultLineFragment(
    int StableId,
    DefaultLineKind Type,
    Point2D P0,
    Point2D P1,
    int EdgeStableId,
    int FirstTriangleIndex,
    int SecondTriangleIndex,
    float StartT,
    float EndT,
    float Depth);
