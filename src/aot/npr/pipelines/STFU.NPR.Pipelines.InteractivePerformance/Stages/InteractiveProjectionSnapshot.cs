using STFU.NPR.Graph;
using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;

namespace STFU.NPR.Pipeline.InteractivePerformance.Stages;

internal sealed record InteractiveProjectionSnapshot(
    NprGraph Graph,
    InteractiveProjectionSource Source,
    int SourceEntityCount,
    int InputMeshCount,
    int InputVertexCount,
    int InputTriangleCount,
    int ProjectedMeshCount,
    int ProjectedVertexCount,
    int ProjectedTriangleCount,
    string Note,
    bool UsedReferenceGraph = false);
