using STFU.NPR.Composition;
using STFU.NPR.Pipeline.ComicSurface;
using STFU.NPR.Pipeline.InteractivePerformance;
using STFU.NPR.Pipeline.ReferenceQuality;

namespace STFU.NPR.Pipelines;

public static class BuiltInNprPipelines
{
    public static IReadOnlyList<INprPipelineProvider> CreateAll()
    {
        return
        [
            new ReferenceQualityPipelineProvider(),
            new InteractivePerformancePipelineProvider(),
            new ComicSurfacePipelineProvider()
        ];
    }
}
