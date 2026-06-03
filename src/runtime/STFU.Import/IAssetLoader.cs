using STFU.Abstractions.Loading;
using STFU.Assets;

namespace STFU.Import;

public interface IAssetLoader<TSource> : ILoader<TSource, ImportedAsset>
{
}
