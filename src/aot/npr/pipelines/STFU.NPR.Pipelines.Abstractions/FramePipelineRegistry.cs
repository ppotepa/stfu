using STFU.NPR.Composition;

namespace STFU.NPR.Pipelines.Abstractions;

public sealed class FramePipelineRegistry : IFramePipelineRegistry
{
    private readonly Dictionary<FramePipelineStrategy, IFramePipelineStrategyProvider> _providers;

    public FramePipelineRegistry(IEnumerable<IFramePipelineStrategyProvider> providers)
    {
        _providers = providers.ToDictionary(provider => provider.Strategy);
    }

    public IReadOnlyList<IFramePipelineStrategyProvider> StrategyProviders => _providers.Values.ToArray();

    public IFramePipelineStrategyProvider Get(FramePipelineStrategy strategy)
    {
        if (_providers.TryGetValue(strategy, out var provider))
        {
            return provider;
        }

        throw new KeyNotFoundException($"Frame pipeline strategy '{strategy}' is not registered.");
    }

    public INprPipelineProvider GetProvider(FramePipelineStrategy strategy)
    {
        return Get(strategy);
    }
}
