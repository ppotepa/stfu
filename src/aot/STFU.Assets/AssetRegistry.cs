using STFU.Common.Primitives;
using STFU.Mesh;

namespace STFU.Assets;

public sealed class AssetRegistry
{
    private readonly Dictionary<MeshHandle, MeshData> _meshes = new();
    private readonly Dictionary<string, MeshHandle> _meshesByPath = new(StringComparer.OrdinalIgnoreCase);
    private int _nextMeshId;

    public int MeshCount => _meshes.Count;

    public MeshHandle AddMesh(string path, MeshData mesh)
    {
        var handle = new MeshHandle(++_nextMeshId);
        _meshes[handle] = mesh;
        _meshesByPath[path] = handle;
        return handle;
    }

    public bool TryGetMesh(MeshHandle handle, out MeshData mesh)
    {
        return _meshes.TryGetValue(handle, out mesh!);
    }

    public bool TryGetMeshHandle(string path, out MeshHandle handle)
    {
        return _meshesByPath.TryGetValue(path, out handle);
    }
}
