using STFU.Messaging.Commands;
using STFU.Viewport.Commands;

namespace STFU.Viewport.Handlers;

public sealed class SetViewportDebugOverlayCommandHandler : ICommandHandler<SetViewportDebugOverlayCommand>
{
    private readonly ViewportState _viewport;

    public SetViewportDebugOverlayCommandHandler(ViewportState viewport)
    {
        _viewport = viewport;
    }

    public void Handle(SetViewportDebugOverlayCommand command)
    {
        _viewport.SetDebugOverlay(command.Overlay);
    }
}
