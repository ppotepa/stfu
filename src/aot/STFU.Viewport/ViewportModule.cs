using STFU.Engine.Composition;
using STFU.NPR.Debug;
using STFU.Strokes;
using STFU.Viewport.Commands;
using STFU.Viewport.Handlers;

namespace STFU.Viewport;

public sealed class ViewportModule : IEngineModule
{
    public void Register(EngineModuleContext context)
    {
        var state = new ViewportState();
        var debug = context.Services.GetRequired<NprDebugState>();
        var strokes = context.Services.GetRequired<StrokeState>();

        context.Services.AddSingleton(state);
        context.Commands
            .Register(new SetViewportSizeCommandHandler(state))
            .Register(new SetViewportDebugOverlayCommandHandler(state))
            .Register(new SetViewportRenderModeCommandHandler(state))
            .Register(new RequestRenderCommandHandler(state, debug, strokes));
    }
}
