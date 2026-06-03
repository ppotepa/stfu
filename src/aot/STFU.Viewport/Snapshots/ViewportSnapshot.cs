using STFU.Messaging.Snapshots;
using STFU.Strokes;

namespace STFU.Viewport.Snapshots;

public sealed record ViewportSnapshot(
    int Width,
    int Height,
    ViewportRenderMode RenderMode,
    StrokeFrame Frame) : ISnapshot;
