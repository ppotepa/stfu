using STFU.Messaging.Commands;

namespace STFU.Projection.Commands;

public readonly record struct SetCameraCommand(CameraState Camera) : ICommand;
