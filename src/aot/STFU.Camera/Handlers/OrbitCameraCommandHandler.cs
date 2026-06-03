using STFU.Camera.Commands;
using STFU.Messaging.Commands;

namespace STFU.Camera.Handlers;

public sealed class OrbitCameraCommandHandler : ICommandHandler<OrbitCameraCommand>
{
    private readonly CameraRig _camera;

    public OrbitCameraCommandHandler(CameraRig camera)
    {
        _camera = camera;
    }

    public void Handle(OrbitCameraCommand command)
    {
        _camera.Orbit(command.DeltaYawRadians, command.DeltaPitchRadians);
    }
}
