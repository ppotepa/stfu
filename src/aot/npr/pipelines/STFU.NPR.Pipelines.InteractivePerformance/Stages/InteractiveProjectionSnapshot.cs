using STFU.NPR.Graph;
using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;

namespace STFU.NPR.Pipeline.InteractivePerformance.Stages;

internal sealed record InteractiveProjectionSnapshot(
    NprGraph Graph,
    InteractiveProjectionSource Source,
    int SourceEntityCount,
    int ProjectedMeshCount,
    int ProjectedVertexCount,
    int ProjectedTriangleCount,
    string Note);
