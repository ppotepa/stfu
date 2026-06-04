using STFU.Abstractions.Modules;

namespace STFU.Strokes;

public sealed class StrokesModule : IEngineModule
{
    public void Register(IModuleContext context)
    {
        context.Services.AddSingleton(new StrokeState());
    }
}
