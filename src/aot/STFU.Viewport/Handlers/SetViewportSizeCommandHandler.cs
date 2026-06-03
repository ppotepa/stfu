using STFU.Messaging.Commands;
using STFU.Viewport.Commands;

namespace STFU.Viewport.Handlers;

public sealed class SetViewportSizeCommandHandler : ICommandHandler<SetViewportSizeCommand>
{
    private readonly ViewportState _state;

    public SetViewportSizeCommandHandler(ViewportState state)
    {
        _state = state;
    }

    public void Handle(SetViewportSizeCommand command)
    {
        _state.Resize(command.Width, command.Height);
    }
}
