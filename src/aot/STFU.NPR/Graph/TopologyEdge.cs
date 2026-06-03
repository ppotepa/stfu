namespace STFU.NPR.Graph;

public readonly record struct TopologyEdge(
    int StableId,
    int StartVertexIndex,
    int EndVertexIndex,
    int FirstTriangleIndex,
    int SecondTriangleIndex,
    float NormalAngleDegrees,
    bool IsBoundary);
