namespace STFU.NPR.Analysis;

public readonly record struct TileDensityBudget(
    (int X, int Y) Tile,
    int AllowedCount,
    int CandidateCount);
