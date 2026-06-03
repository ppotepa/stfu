using STFU.Messaging.Commands;
using STFU.Projection.Commands;

namespace STFU.Projection.Handlers;

public sealed class SetCameraCommandHandler : ICommandHandler<SetCameraCommand>
{
    private readonly ProjectionState _state;

    public SetCameraCommandHandler(ProjectionState state)
    {
        _state = state;
    }

    public void Handle(SetCameraCommand command)
    {
        _state.SetCamera(command.Camera);
    }
}
