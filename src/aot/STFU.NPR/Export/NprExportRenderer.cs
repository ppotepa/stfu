using STFU.NPR.Pipeline;
using STFU.NPR.Visibility;

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
            Style = sourceContext.Style with
            {
                Visibility = sourceContext.Style.Visibility with
                {
                    Strictness = Composition.VisibilityStrictness.OfflineExact
                }
            },
            Analysis = sourceContext.Analysis,
            VisibilityResolver = new OfflineExactVisibilityResolver(),
            OcclusionQuery = CloneOcclusionQuery(sourceContext.OcclusionQuery),
            FrameHistoryState = new Temporal.FrameHistoryState(),
            FrameId = sourceContext.FrameId,
            TimeSeconds = sourceContext.TimeSeconds,
            PreviousFrame = sourceContext.PreviousFrame
        };

        pipeline.Execute(exportContext);
        return exportContext;
    }

    private static IOcclusionQuery CloneOcclusionQuery(IOcclusionQuery query)
    {
        return query switch
        {
            BvhOcclusionQuery => new BvhOcclusionQuery(),
            SampleOcclusionQuery => new SampleOcclusionQuery(),
            _ => new BvhOcclusionQuery()
        };
    }
}
