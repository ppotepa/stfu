namespace STFU.Mesh;

public sealed record MeshData(
    IReadOnlyList<MeshVertex> Vertices,
    IReadOnlyList<MeshTriangle> Triangles,
    IReadOnlyList<int>? LogicalVertexIds = null)
{
    public static MeshData Empty { get; } = new([], []);
}
