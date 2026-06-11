using STFU.Common.Math;
using STFU.Common.Primitives;
using STFU.Mesh;

namespace STFU.NPR.Pipeline.InteractivePerformance.Stages;

internal sealed record InteractiveProjectionInputMesh(
    EntityId EntityId,
    MeshHandle MeshHandle,
    MeshData Mesh,
    Transform3D Transform,
    int EntityIndex,
    string Role)
{
    public int VertexCount => Mesh.Vertices.Count;

    public int TriangleCount => Mesh.Triangles.Count;

    public bool HasGeometry => VertexCount > 0 || TriangleCount > 0;
}
