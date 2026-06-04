using STFU.Common.Primitives;
using STFU.Animation.Clips;
using STFU.Animation.Skeleton;
using STFU.Animation.Skinning;
using STFU.Mesh;

namespace STFU.Assets;

public sealed class AssetRegistry
{
    private readonly Dictionary<MeshHandle, MeshData> _meshes = new();
    private readonly Dictionary<SkinnedMeshHandle, SkinnedMeshData> _skinnedMeshes = new();
    private readonly Dictionary<SkeletonHandle, SkeletonData> _skeletons = new();
    private readonly Dictionary<AnimationClipHandle, AnimationClip> _animationClips = new();
    private readonly Dictionary<string, MeshHandle> _meshesByPath = new(StringComparer.OrdinalIgnoreCase);
    private int _nextMeshId;
    private int _nextSkinnedMeshId;
    private int _nextSkeletonId;
    private int _nextAnimationClipId;

    public int MeshCount => _meshes.Count;

    public IEnumerable<AssetMeshEntry> MeshEntries => _meshesByPath
        .Select(entry => new AssetMeshEntry(entry.Key, entry.Value, _meshes[entry.Value]));

    public int SkinnedMeshCount => _skinnedMeshes.Count;

    public int SkeletonCount => _skeletons.Count;

    public int AnimationClipCount => _animationClips.Count;

    public MeshHandle AddMesh(string path, MeshData mesh)
    {
        var handle = new MeshHandle(++_nextMeshId);
        _meshes[handle] = mesh;
        _meshesByPath[path] = handle;
        return handle;
    }

    public SkinnedMeshHandle AddSkinnedMesh(SkinnedMeshData mesh)
    {
        var handle = new SkinnedMeshHandle(++_nextSkinnedMeshId);
        _skinnedMeshes[handle] = mesh;
        return handle;
    }

    public SkeletonHandle AddSkeleton(SkeletonData skeleton)
    {
        var handle = new SkeletonHandle(++_nextSkeletonId);
        _skeletons[handle] = skeleton;
        return handle;
    }

    public AnimationClipHandle AddAnimationClip(AnimationClip clip)
    {
        var handle = new AnimationClipHandle(++_nextAnimationClipId);
        _animationClips[handle] = clip;
        return handle;
    }

    public bool TryGetMesh(MeshHandle handle, out MeshData mesh)
    {
        return _meshes.TryGetValue(handle, out mesh!);
    }

    public bool TryGetSkinnedMesh(SkinnedMeshHandle handle, out SkinnedMeshData mesh)
    {
        return _skinnedMeshes.TryGetValue(handle, out mesh!);
    }

    public bool TryGetSkeleton(SkeletonHandle handle, out SkeletonData skeleton)
    {
        return _skeletons.TryGetValue(handle, out skeleton!);
    }

    public bool TryGetAnimationClip(AnimationClipHandle handle, out AnimationClip clip)
    {
        return _animationClips.TryGetValue(handle, out clip!);
    }

    public bool TryGetMeshHandle(string path, out MeshHandle handle)
    {
        return _meshesByPath.TryGetValue(path, out handle);
    }
}

public sealed record AssetMeshEntry(
    string Path,
    MeshHandle Handle,
    MeshData Mesh);
