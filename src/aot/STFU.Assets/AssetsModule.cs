using STFU.Engine.Composition;

namespace STFU.Assets;

public sealed class AssetsModule : IEngineModule
{
    public void Register(EngineModuleContext context)
    {
        context.Services.AddSingleton(new AssetRegistry());
    }
}
