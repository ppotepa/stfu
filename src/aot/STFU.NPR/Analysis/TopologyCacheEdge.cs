namespace STFU.NPR.Analysis;

public readonly record struct TopologyCacheEdge(
    int StableId,
    int StartVertexIndex,
    int EndVertexIndex,
    int FirstTriangleIndex,
    int SecondTriangleIndex,
    bool IsBoundary,
    float NormalAngleDegrees,
    EdgeSemantic Semantic);
