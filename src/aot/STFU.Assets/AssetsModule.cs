using STFU.Abstractions.Modules;

namespace STFU.Assets;

public sealed class AssetsModule : IEngineModule
{
    public void Register(IModuleContext context)
    {
        context.Services.AddSingleton(new AssetRegistry());
    }
}
