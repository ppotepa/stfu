using STFU.NPR.Pipeline.ReferenceQuality;

namespace STFU.NPR.Pipeline.InteractivePerformance;

public static class InteractivePerformancePipeline
{
    public static STFU.NPR.Pipeline.INprPipeline Create()
    {
        // Temporary bootstrap: Interactive Performance is selectable now,
        // but uses Reference Quality until the optimized pipeline is implemented.
        return ReferenceQualityPipeline.Create();
    }
}
