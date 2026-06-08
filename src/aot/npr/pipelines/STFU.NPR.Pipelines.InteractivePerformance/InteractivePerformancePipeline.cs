using STFU.NPR.Pipelines.Abstractions;

namespace STFU.NPR.Pipeline.InteractivePerformance;

public static class InteractivePerformancePipeline
{
    public static STFU.NPR.Pipeline.INprPipeline Create()
    {
        return Create(FramePipelineStrategyOptions.Default);
    }

    public static STFU.NPR.Pipeline.INprPipeline Create(FramePipelineStrategyOptions options)
    {
        return new InteractivePerformanceNprPipeline(options);
    }
}
