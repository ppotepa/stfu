using STFU.Messaging.Commands;

namespace STFU.Camera.Commands;

public readonly record struct AdjustCameraFovCommand(float DeltaDegrees) : ICommand;
