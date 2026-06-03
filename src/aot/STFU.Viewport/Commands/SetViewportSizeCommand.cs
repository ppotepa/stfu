using STFU.Messaging.Commands;

namespace STFU.Viewport.Commands;

public readonly record struct SetViewportSizeCommand(
    int Width,
    int Height) : ICommand;
