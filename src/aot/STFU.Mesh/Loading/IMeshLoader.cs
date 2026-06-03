using STFU.Abstractions.Loading;

namespace STFU.Mesh.Loading;

public interface IMeshLoader<TSource> : ILoader<TSource, MeshData>
{
}
