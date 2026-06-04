using STFU.NPR.Composition;
using STFU.NPR.Pipeline.ComicSurface;
using STFU.NPR.Pipeline.Default;

namespace STFU.NPR.Pipelines;

public static class BuiltInNprPipelines
{
    public static IReadOnlyList<INprPipelineProvider> CreateAll()
    {
        return
        [
            new DefaultPipelineProvider(),
            new ComicSurfacePipelineProvider()
        ];
    }
}
