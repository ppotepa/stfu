using STFU.Animation.Clips;
using STFU.Animation.Skeleton;
using STFU.Animation.Skinning;
using STFU.Mesh;

namespace STFU.Assets;

public sealed record ImportedAsset(
    string SourcePath,
    IReadOnlyList<ImportedMesh> Meshes,
    IReadOnlyList<ImportedSkinnedMesh> SkinnedMeshes,
    IReadOnlyList<SkeletonData> Skeletons,
    IReadOnlyList<AnimationClip> Animations,
    IReadOnlyDictionary<string, string> Metadata)
{
    public static ImportedAsset Empty(string sourcePath) => new(
        sourcePath,
        [],
        [],
        [],
        [],
        new Dictionary<string, string>(StringComparer.Ordinal));
}

public sealed record ImportedMesh(
    string Name,
    MeshData Mesh);

public sealed record ImportedSkinnedMesh(
    string Name,
    MeshData BindMesh,
    IReadOnlyList<VertexSkinWeights> SkinWeights,
    int SkeletonIndex);
