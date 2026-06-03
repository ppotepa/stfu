using STFU.Camera.Commands;
using STFU.Messaging.Commands;

namespace STFU.Camera.Handlers;

public sealed class PanCameraCommandHandler : ICommandHandler<PanCameraCommand>
{
    private readonly CameraRig _camera;

    public PanCameraCommandHandler(CameraRig camera)
    {
        _camera = camera;
    }

    public void Handle(PanCameraCommand command)
    {
        _camera.Pan(command.DeltaRight, command.DeltaUp);
    }
}
