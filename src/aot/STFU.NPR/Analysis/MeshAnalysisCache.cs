namespace STFU.NPR.Analysis;

public sealed record MeshAnalysisCache(
    TopologyCache Topology,
    MeshBounds Bounds,
    CurvatureCache? Curvature);
