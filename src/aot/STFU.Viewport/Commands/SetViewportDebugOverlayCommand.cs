using STFU.Messaging.Commands;
using STFU.NPR.Debug;

namespace STFU.Viewport.Commands;

public readonly record struct SetViewportDebugOverlayCommand(DebugOverlayKind Overlay) : ICommand;
