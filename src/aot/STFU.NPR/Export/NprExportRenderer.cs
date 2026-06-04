using STFU.NPR.Pipeline;

namespace STFU.NPR.Export;

public sealed class NprExportRenderer
{
    public NprContext RenderOfflineExact(INprPipeline pipeline, NprContext sourceContext)
    {
        ArgumentNullException.ThrowIfNull(pipeline);
        ArgumentNullException.ThrowIfNull(sourceContext);

        var exportContext = new NprContext
        {
            Scene = sourceContext.Scene,
            Assets = sourceContext.Assets,
            Camera = sourceContext.Camera,
            Width = sourceContext.Width,
            Height = sourceContext.Height,
            Settings = sourceContext.Settings,
            Style = sourceContext.Style,
            StyleSet = sourceContext.StyleSet,
            EntityStyles = sourceContext.EntityStyles,
            Analysis = sourceContext.Analysis,
            FrameHistoryState = new Temporal.FrameHistoryState(),
            FrameId = sourceContext.FrameId,
            TimeSeconds = sourceContext.TimeSeconds,
            PreviousFrame = sourceContext.PreviousFrame
        };

        pipeline.Execute(exportContext);
        return exportContext;
    }
}
