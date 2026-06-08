using STFU.NPR.Composition;
using STFU.NPR.Pipelines.Abstractions;

namespace STFU.NPR.Pipeline.InteractivePerformance;

public sealed class InteractivePerformancePipelineProvider : IFramePipelineStrategyProvider
{
    public FramePipelineStrategy Strategy => FramePipelineStrategy.InteractivePerformance;

    public string PipelineId => NprPipelineIds.InteractivePerformance;

    public FramePipelineDescriptor Descriptor { get; } = new(
        FramePipelineStrategy.InteractivePerformance,
        NprPipelineIds.InteractivePerformance,
        "Interactive Performance",
        "Optimized realtime pipeline using cache-aware artifacts, budgeted updates and direct GPU-first rendering.");

    public IReadOnlyList<INprPreset> CreateBuiltInPresets()
    {
        return [];
    }

    public STFU.NPR.Pipeline.INprPipeline CreatePipeline(FramePipelineStrategyOptions options)
    {
        return InteractivePerformancePipeline.Create(options);
    }
}
