using STFU.NPR.Composition;
using STFU.NPR.Pipeline.ReferenceQuality;

namespace STFU.NPR.Pipeline.Default;

public sealed class DefaultPipelineProvider : INprPipelineProvider
{
    private readonly ReferenceQualityPipelineProvider _inner = new();

    public string PipelineId => NprPipelineIds.Default;

    public IReadOnlyList<INprPreset> CreateBuiltInPresets()
    {
        return _inner.CreateBuiltInPresets();
    }

    public STFU.NPR.Pipeline.INprPipeline CreatePipeline()
    {
        return _inner.CreatePipeline();
    }
}
