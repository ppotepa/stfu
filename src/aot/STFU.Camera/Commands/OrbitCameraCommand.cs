using STFU.Messaging.Commands;

namespace STFU.Camera.Commands;

public readonly record struct OrbitCameraCommand(
    float DeltaYawRadians,
    float DeltaPitchRadians) : ICommand;
