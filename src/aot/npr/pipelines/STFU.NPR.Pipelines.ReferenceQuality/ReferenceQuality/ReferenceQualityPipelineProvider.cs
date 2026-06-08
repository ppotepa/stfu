using STFU.NPR.Composition;

using STFU.NPR.Pipelines.Abstractions;

namespace STFU.NPR.Pipeline.ReferenceQuality;

public sealed class ReferenceQualityPipelineProvider : IFramePipelineStrategyProvider
{
    public FramePipelineStrategy Strategy => FramePipelineStrategy.ReferenceQuality;

    public string PipelineId => NprPipelineIds.ReferenceQuality;

    public FramePipelineDescriptor Descriptor { get; } = new(
        FramePipelineStrategy.ReferenceQuality,
        NprPipelineIds.ReferenceQuality,
        "Reference Quality",
        "Full reference NPR pipeline used for validation, export, parity, and highest-quality rendering.");

    public IReadOnlyList<INprPreset> CreateBuiltInPresets()
    {
        return [new DefaultNprPreset()];
    }

    public STFU.NPR.Pipeline.INprPipeline CreatePipeline(FramePipelineStrategyOptions options)
    {
        return ReferenceQualityPipeline.Create();
    }
}