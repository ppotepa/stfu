using STFU.NPR.Composition;
using STFU.NPR.Pipeline;
using STFU.NPR.Pipelines;
using STFU.NPR.Pipelines.Abstractions;
using STFU.Rendering.Abstractions.Execution;
using STFU.Rendering.Abstractions.Requests;

namespace STFU.UI;

internal readonly record struct ViewportFramePipelineSelection(
    INprPipeline? Pipeline,
    string PipelineId,
    FramePipelineStrategy Strategy,
    FramePipelineStrategyOptions Options,
    string Reason);

internal readonly record struct ViewportFramePipelineCacheKey(
    FramePipelineStrategy Strategy,
    bool ForceReferenceFallback,
    bool UseReferenceFallbackForFinalFrame,
    bool EnableInteractivePreviewOutput,
    bool RequireToneCoverageForInteractivePreview,
    int InteractivePreviewMaxStrokeSegments,
    bool EnableSelfContainedProjection,
    bool PreferSelfContainedProjection,
    bool EnableProjectedTriangleVisibility,
    bool EnableInteractiveOutputContract);

internal static class ViewportFramePipelineStrategyOptionsFactory
{
    public static FramePipelineStrategyOptions Create(RendererRuntimePlan runtimePlan)
    {
        return runtimePlan.PipelineStrategy switch
        {
            FramePipelineStrategy.InteractivePerformance => CreateInteractivePerformanceOptions(runtimePlan),
            _ => FramePipelineStrategyOptions.Default
        };
    }

    private static FramePipelineStrategyOptions CreateInteractivePerformanceOptions(RendererRuntimePlan runtimePlan)
    {
        return ViewportInteractivePerformanceOptionsResolver.Create(runtimePlan);
    }
}

internal sealed class ViewportFramePipelineSelector
{
    private readonly IFramePipelineRegistry _strategyRegistry;
    private readonly Dictionary<ViewportFramePipelineCacheKey, INprPipeline> _strategyPipelines = new();

    public ViewportFramePipelineSelector()
        : this(BuiltInFramePipelineStrategies.CreateRegistry())
    {
    }

    internal ViewportFramePipelineSelector(IFramePipelineRegistry strategyRegistry)
    {
        _strategyRegistry = strategyRegistry ?? throw new ArgumentNullException(nameof(strategyRegistry));
    }

    public ViewportFramePipelineSelection Select(
        NprRenderContentKind contentKind,
        ActiveNprPresetState presetState,
        RendererRuntimePlan runtimePlan)
    {
        ArgumentNullException.ThrowIfNull(presetState);

        if (contentKind != NprRenderContentKind.NprPipeline)
        {
            return new ViewportFramePipelineSelection(
                Pipeline: null,
                PipelineId: presetState.ActivePreset.PipelineId,
                Strategy: runtimePlan.PipelineStrategy,
                Options: FramePipelineStrategyOptions.Default,
                Reason: "Mesh wireframe rendering bypasses NPR frame pipeline strategy selection.");
        }

        if (runtimePlan.PipelineStrategy == FramePipelineStrategy.ReferenceQuality)
        {
            return new ViewportFramePipelineSelection(
                Pipeline: presetState.ActivePipeline,
                PipelineId: presetState.ActivePreset.PipelineId,
                Strategy: FramePipelineStrategy.ReferenceQuality,
                Options: FramePipelineStrategyOptions.Default,
                Reason: "Reference Quality uses the active preset pipeline instance.");
        }

        var options = ViewportFramePipelineStrategyOptionsFactory.Create(runtimePlan);
        var provider = _strategyRegistry.Get(runtimePlan.PipelineStrategy);
        var pipeline = ResolvePipeline(provider, options);

        return new ViewportFramePipelineSelection(
            Pipeline: pipeline,
            PipelineId: provider.PipelineId,
            Strategy: provider.Strategy,
            Options: options,
            Reason: $"Renderer runtime selected {provider.Descriptor.DisplayName}.");
    }

    private INprPipeline ResolvePipeline(
        IFramePipelineStrategyProvider provider,
        FramePipelineStrategyOptions options)
    {
        var key = CreateKey(provider.Strategy, options);
        if (_strategyPipelines.TryGetValue(key, out var pipeline))
        {
            return pipeline;
        }

        pipeline = provider.CreatePipeline(options);
        _strategyPipelines[key] = pipeline;
        return pipeline;
    }

    private static ViewportFramePipelineCacheKey CreateKey(
        FramePipelineStrategy strategy,
        FramePipelineStrategyOptions options)
    {
        return new ViewportFramePipelineCacheKey(
            strategy,
            options.ForceReferenceFallback,
            options.UseReferenceFallbackForFinalFrame,
            options.EnableInteractivePreviewOutput,
            options.RequireToneCoverageForInteractivePreview,
            options.InteractivePreviewMaxStrokeSegments,
            options.EnableSelfContainedProjection,
            options.PreferSelfContainedProjection,
            options.EnableProjectedTriangleVisibility,
            options.EnableInteractiveOutputContract);
    }
}