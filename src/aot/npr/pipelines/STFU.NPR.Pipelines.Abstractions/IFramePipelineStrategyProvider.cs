using STFU.NPR.Composition;
using STFU.NPR.Pipeline;

namespace STFU.NPR.Pipelines.Abstractions;

public interface IFramePipelineStrategyProvider : INprPipelineProvider
{
    FramePipelineStrategy Strategy { get; }

    FramePipelineDescriptor Descriptor { get; }

    INprPipeline CreatePipeline(FramePipelineStrategyOptions options);

    INprPipeline INprPipelineProvider.CreatePipeline()
    {
        return CreatePipeline(FramePipelineStrategyOptions.Default);
    }
}
