namespace STFU.NPR.Graph;

public readonly record struct ProjectedEdge(
    int StableId,
    int StartVertexIndex,
    int EndVertexIndex,
    float Depth);
