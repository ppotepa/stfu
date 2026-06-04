using STFU.Common.Primitives;
using STFU.Mesh;

namespace STFU.NPR.Graph;

public sealed record ProjectedMesh(
    EntityId EntityId,
    MeshHandle MeshHandle,
    MeshData Mesh,
    int VertexOffset,
    int VertexCount,
    int TriangleOffset,
    int TriangleCount);
