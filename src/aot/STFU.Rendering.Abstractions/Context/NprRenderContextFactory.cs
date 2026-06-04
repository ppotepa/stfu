using STFU.NPR.Graph;
using STFU.NPR.Pipeline;
using STFU.Rendering.Abstractions.Requests;

namespace STFU.Rendering.Abstractions.Context;

public static class NprRenderContextFactory
{
    public static NprContext Create(NprRenderRequest request, NprGraph? graph = null)
    {
        return new NprContext
        {
            FrameId = request.FrameId,
            TimeSeconds = request.TimeSeconds,
            PreviousFrame = request.PreviousFrame,
            IncludeDebugFrame = request.IncludeDebugFrame,
            Scene = request.Scene,
            Assets = request.Assets,
            Camera = request.Camera,
            Width = request.Width,
            Height = request.Height,
            Settings = NprSettingsCloner.Clone(request.Settings),
            Style = request.Style,
            StyleSet = request.StyleSet,
            EntityStyles = request.EntityStyles,
            Analysis = request.Analysis,
            FrameHistoryState = request.FrameHistoryState,
            Graph = graph ?? new NprGraph()
        };
    }
}
