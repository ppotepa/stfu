using STFU.Abstractions.Loading;
using STFU.Mesh.Loading;

namespace STFU.Mesh;

public sealed class MeshFactory
{
    public MeshData Load<TSource>(
        TSource source,
        IMeshLoader<TSource> loader,
        LoadContext? context = null)
    {
        var result = loader.Load(source, context ?? LoadContext.Default);
        return result.GetValueOrThrow();
    }
}
