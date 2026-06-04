using STFU.NPR.Composition;
using STFU.NPR.Pipeline;

namespace STFU.NPR.Pipeline.ComicSurface;

public sealed class ComicSurfacePipelineProvider : INprPipelineProvider
{
    public string PipelineId => NprPipelineIds.ComicSurface;

    public INprPipeline CreatePipeline()
    {
        return ComicSurfacePipeline.Create();
    }
}
