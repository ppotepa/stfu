using STFU.NPR.Pipeline;

namespace STFU.NPR.Pipeline.ComicSurface;

public static class ComicSurfacePipeline
{
    public static INprPipeline Create()
    {
        return new NprPipeline([]);
    }
}
