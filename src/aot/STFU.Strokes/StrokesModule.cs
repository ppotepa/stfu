using STFU.Engine.Composition;

namespace STFU.Strokes;

public sealed class StrokesModule : IEngineModule
{
    public void Register(EngineModuleContext context)
    {
        context.Services.AddSingleton(new StrokeState());
    }
}
