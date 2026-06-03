using STFU.Camera.Commands;
using STFU.Messaging.Commands;

namespace STFU.Camera.Handlers;

public sealed class AdjustCameraFovCommandHandler : ICommandHandler<AdjustCameraFovCommand>
{
    private readonly CameraRig _camera;

    public AdjustCameraFovCommandHandler(CameraRig camera)
    {
        _camera = camera;
    }

    public void Handle(AdjustCameraFovCommand command)
    {
        _camera.AdjustFieldOfView(command.DeltaDegrees);
    }
}
