using STFU.NPR.Composition;

namespace STFU.NPR.Pipeline.Default;

public sealed class DefaultPipelineProvider : INprPipelineProvider
{
    public string PipelineId => NprPipelineIds.Default;

    public IReadOnlyList<INprPreset> CreateBuiltInPresets()
    {
        return [new DefaultNprPreset()];
    }

    public STFU.NPR.Pipeline.INprPipeline CreatePipeline()
    {
        return DefaultPipeline.Create();
    }
}
