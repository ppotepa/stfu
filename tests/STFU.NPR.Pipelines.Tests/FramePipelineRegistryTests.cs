using STFU.NPR.Composition;
using STFU.NPR.Pipeline.InteractivePerformance;
using STFU.NPR.Pipeline.ReferenceQuality;
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

    [Fact]
    public void BuiltInNprPipelines_registers_strategy_pipeline_providers()
    {
        var providers = BuiltInNprPipelines.CreateAll();

        Assert.Contains(providers, provider => provider.PipelineId == NprPipelineIds.ReferenceQuality);
        Assert.Contains(providers, provider => provider.PipelineId == NprPipelineIds.InteractivePerformance);
    }

    [Fact]
    public void BuiltInNprPipelines_interactive_provider_does_not_add_presets()
    {
        var providers = BuiltInNprPipelines.CreateAll();
        var provider = Assert.IsType<InteractivePerformancePipelineProvider>(
            providers.Single(item => item.PipelineId == NprPipelineIds.InteractivePerformance));

        Assert.Empty(provider.CreateBuiltInPresets());
    }

    [Fact]
    public void BuiltInNprPipelines_can_create_reference_and_interactive_instances()
    {
        var providers = BuiltInNprPipelines.CreateAll();
        var reference = providers.Single(item => item.PipelineId == NprPipelineIds.ReferenceQuality);
        var interactive = providers.Single(item => item.PipelineId == NprPipelineIds.InteractivePerformance);

        Assert.NotNull(reference.CreatePipeline());
        Assert.NotNull(interactive.CreatePipeline());
    }

    [Fact]
    public void Strategy_registry_creates_interactive_pipeline_with_safe_viewport_defaults()
    {
        var registry = BuiltInFramePipelineStrategies.CreateRegistry();
        var provider = registry.Get(FramePipelineStrategy.InteractivePerformance);
        var options = FramePipelineStrategyOptions.Default with
        {
            EnableInteractivePreviewOutput = false,
            UseReferenceFallbackForFinalFrame = true,
            PreferSelfContainedProjection = true
        };

        var pipeline = provider.CreatePipeline(options);

        Assert.IsType<InteractivePerformanceNprPipeline>(pipeline);
    }

}
