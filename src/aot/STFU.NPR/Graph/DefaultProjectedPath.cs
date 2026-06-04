using STFU.Strokes;

namespace STFU.NPR.Graph;

public sealed record DefaultProjectedPath(
    int StableId,
    DefaultLineKind Type,
    IReadOnlyList<Point2D> Points,
    int PathIndex,
    float Length);
