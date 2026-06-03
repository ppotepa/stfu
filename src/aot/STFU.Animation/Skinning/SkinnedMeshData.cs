using STFU.Common.Primitives;
using STFU.Mesh;

namespace STFU.Animation.Skinning;

public sealed record SkinnedMeshData(
    MeshData BindMesh,
    IReadOnlyList<VertexSkinWeights> SkinWeights,
    SkeletonHandle Skeleton)
{
    public bool HasSkinWeights => SkinWeights.Count == BindMesh.Vertices.Count;
}
