using STFU.Abstractions.Loading;
using STFU.Assets;

namespace STFU.Import;

public sealed class AssetImportFactory
{
    public ImportedAsset Load<TSource>(
        TSource source,
        IAssetLoader<TSource> loader,
        LoadContext? context = null)
    {
        var result = loader.Load(source, context ?? LoadContext.Default);
        return result.GetValueOrThrow();
    }
}
