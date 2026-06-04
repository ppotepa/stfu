namespace STFU.NPR.Analysis;

public sealed record TopologyCache(
    IReadOnlyList<TopologyCacheEdge> Edges);
