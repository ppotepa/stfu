using STFU.Messaging.Commands;

namespace STFU.Camera.Commands;

public readonly record struct SetCameraCommand(CameraState Camera) : ICommand;
