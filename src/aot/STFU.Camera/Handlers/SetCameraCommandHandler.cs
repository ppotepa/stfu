using STFU.Messaging.Commands;
using STFU.Camera.Commands;

namespace STFU.Camera.Handlers;

public sealed class SetCameraCommandHandler : ICommandHandler<SetCameraCommand>
{
    private readonly CameraRig _state;

    public SetCameraCommandHandler(CameraRig state)
    {
        _state = state;
    }

    public void Handle(SetCameraCommand command)
    {
        _state.SetCamera(command.Camera);
    }
}
