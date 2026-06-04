using STFU.Common.Primitives;

namespace STFU.NPR.Graph;

public readonly record struct TopologyEdge(
    int StableId,
    int StartVertexIndex,
    int EndVertexIndex,
    int FirstTriangleIndex,
    int SecondTriangleIndex,
    float NormalAngleDegrees,
    bool IsBoundary)
{
    public EntityId EntityId { get; init; } = EntityId.None;
}
