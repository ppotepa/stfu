using STFU.NPR.Composition;

namespace STFU.NPR.Pipelines.Abstractions;

public interface IFramePipelineRegistry
{
    IReadOnlyList<IFramePipelineStrategyProvider> StrategyProviders { get; }

    IFramePipelineStrategyProvider Get(FramePipelineStrategy strategy);

    INprPipelineProvider GetProvider(FramePipelineStrategy strategy);
}
