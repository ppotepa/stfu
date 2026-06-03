using STFU.Messaging.Commands;

namespace STFU.Camera.Commands;

public readonly record struct PanCameraCommand(
    float DeltaRight,
    float DeltaUp) : ICommand;
