using STFU.Messaging.Commands;

namespace STFU.Viewport.Commands;

public readonly record struct SetViewportRenderModeCommand(ViewportRenderMode Mode) : ICommand;
