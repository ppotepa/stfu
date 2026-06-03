using STFU.Messaging.Commands;
using STFU.Viewport.Commands;

namespace STFU.Viewport.Handlers;

public sealed class SetViewportRenderModeCommandHandler : ICommandHandler<SetViewportRenderModeCommand>
{
    private readonly ViewportState _viewport;

    public SetViewportRenderModeCommandHandler(ViewportState viewport)
    {
        _viewport = viewport;
    }

    public void Handle(SetViewportRenderModeCommand command)
    {
        _viewport.SetRenderMode(command.Mode);
    }
}
