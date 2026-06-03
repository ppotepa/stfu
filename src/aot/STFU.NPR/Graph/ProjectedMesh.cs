using STFU.Mesh;

namespace STFU.NPR.Graph;

public sealed record ProjectedMesh(
    MeshData Mesh,
    int VertexOffset,
    int VertexCount,
    int TriangleOffset,
    int TriangleCount);
