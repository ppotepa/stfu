using STFU.Common.Primitives;
using STFU.NPR.Settings;

namespace STFU.NPR.Analysis;

public sealed record DefaultNprTopologyCache(
    IReadOnlyList<DefaultNprTopologyEdge> Edges,
    int SourceTriangleCount,
    int SourceVertexCount,
    ulong Signature,
    DefaultTopologyMode Mode);

public readonly record struct DefaultNprTopologyEdge(
    int StableId,
    int StartVertexIndex,
    int EndVertexIndex,
    int FirstTriangleIndex,
    int SecondTriangleIndex,
    bool IsBoundary,
    int FirstEncounterStartVertexIndex = -1,
    int FirstEncounterEndVertexIndex = -1,
    int SecondEncounterStartVertexIndex = -1,
    int SecondEncounterEndVertexIndex = -1);

internal readonly record struct DefaultNprTopologyCacheKey(
    MeshHandle Handle,
    DefaultTopologyMode Mode);
