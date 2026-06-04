namespace STFU.NPR.Composition;

public sealed record StyleBudgetRule(
    int TileSizePixels,
    int MaxSegmentsPerTile,
    bool AlwaysKeepPrimaryContours);
