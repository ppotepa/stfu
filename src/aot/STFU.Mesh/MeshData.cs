namespace STFU.Mesh;

public sealed record MeshData(
    IReadOnlyList<MeshVertex> Vertices,
    IReadOnlyList<MeshTriangle> Triangles)
{
    public static MeshData Empty { get; } = new([], []);
}
