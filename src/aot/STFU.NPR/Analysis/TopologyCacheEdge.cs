namespace STFU.NPR.Analysis;

public readonly record struct TopologyCacheEdge(
    int StartVertexIndex,
    int EndVertexIndex,
    int FirstTriangleIndex,
    int SecondTriangleIndex,
    bool IsBoundary,
    float NormalAngleDegrees,
    EdgeSemantic Semantic,
    int FirstEncounterStartVertexIndex,
    int FirstEncounterEndVertexIndex);
