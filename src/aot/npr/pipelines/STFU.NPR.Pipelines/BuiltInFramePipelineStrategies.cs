using STFU.NPR.Pipeline.InteractivePerformance;
using STFU.NPR.Pipeline.ReferenceQuality;
using STFU.NPR.Pipelines.Abstractions;

namespace STFU.NPR.Pipelines;

public static class BuiltInFramePipelineStrategies
{
    public static IFramePipelineRegistry CreateRegistry()
    {
        return new FramePipelineRegistry(CreateAll());
    }

    public static IReadOnlyList<IFramePipelineStrategyProvider> CreateAll()
    {
        return
        [
            new ReferenceQualityPipelineProvider(),
            new InteractivePerformancePipelineProvider()
        ];
    }
}
