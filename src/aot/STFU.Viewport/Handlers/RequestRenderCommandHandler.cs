using STFU.Messaging.Commands;
using STFU.NPR.Debug;
using STFU.NPR.Rendering;
using STFU.Strokes;
using STFU.Viewport.Commands;
using STFU.Viewport.Snapshots;

namespace STFU.Viewport.Handlers;

public sealed class RequestRenderCommandHandler : ICommandHandler<RequestRenderCommand>
{
    private readonly ViewportState _viewport;
    private readonly NprDebugState _debug;
    private readonly StrokeState _strokes;
    private readonly NprFrameState _nprFrames;

    public RequestRenderCommandHandler(
        ViewportState viewport,
        NprDebugState debug,
        StrokeState strokes,
        NprFrameState nprFrames)
    {
        _viewport = viewport;
        _debug = debug;
        _strokes = strokes;
        _nprFrames = nprFrames;
    }

    public void Handle(RequestRenderCommand command)
    {
        _viewport.Publish(new ViewportSnapshot(
            _viewport.Width,
            _viewport.Height,
            _viewport.RenderMode,
            _strokes.CurrentFrame,
            _nprFrames.CurrentFrame,
            _debug.CurrentFrame));
    }
}
