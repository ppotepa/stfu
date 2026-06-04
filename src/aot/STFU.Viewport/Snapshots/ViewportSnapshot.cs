using STFU.Messaging.Snapshots;
using STFU.NPR.Debug;
using STFU.NPR.Rendering;
using STFU.Strokes;

namespace STFU.Viewport.Snapshots;

public sealed record ViewportSnapshot(
    int Width,
    int Height,
    ViewportRenderMode RenderMode,
    StrokeFrame Frame,
    NprFrame? NprFrame,
    NprDebugFrame DebugFrame) : ISnapshot;
