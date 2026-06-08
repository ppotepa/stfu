using STFU.NPR.Pipelines.Abstractions;
using Xunit;

namespace STFU.NPR.Pipelines.Tests;

public sealed class FramePipelineRegistryTests
{
    [Fact]
    public void BuiltInFramePipelineStrategies_registers_both_strategies()
    {
        var registry = BuiltInFramePipelineStrategies.CreateRegistry();

        Assert.Equal(FramePipelineStrategy.ReferenceQuality,
            registry.Get(FramePipelineStrategy.ReferenceQuality).Strategy);

        Assert.Equal(FramePipelineStrategy.InteractivePerformance,
            registry.Get(FramePipelineStrategy.InteractivePerformance).Strategy);
    }

    [Fact]
    public void Registry_can_get_provider_by_strategy()
    {
        var all = BuiltInFramePipelineStrategies.CreateAll();
        var registry = new FramePipelineRegistry(all);

        var provider = registry.Get(FramePipelineStrategy.ReferenceQuality);
        
        Assert.NotNull(provider);
        Assert.Equal(FramePipelineStrategy.ReferenceQuality, provider.Strategy);
    }
}
