using STFU.NPR.Pipeline;

namespace STFU.NPR.Composition;

public sealed class NprPipelineRegistry
{
    private readonly Dictionary<string, INprPipelineProvider> _providers = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<INprPipelineProvider> Providers => _providers.Values;

    public void Register(INprPipelineProvider provider)
    {
        _providers[provider.PipelineId] = provider;
    }

    public bool TryCreate(string pipelineId, out INprPipeline pipeline)
    {
        if (_providers.TryGetValue(pipelineId, out var provider))
        {
            pipeline = provider.CreatePipeline();
            return true;
        }

        pipeline = null!;
        return false;
    }
}
