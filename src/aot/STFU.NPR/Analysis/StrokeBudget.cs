namespace STFU.NPR.Analysis;

public readonly record struct StrokeBudget(
    int MaxSegmentsPerTile,
    float MinScreenLength,
    float MaxDensityPerTile);
