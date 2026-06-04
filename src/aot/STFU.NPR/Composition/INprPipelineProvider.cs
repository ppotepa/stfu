using STFU.NPR.Pipeline;

namespace STFU.NPR.Composition;

public interface INprPipelineProvider
{
    string PipelineId { get; }

    INprPipeline CreatePipeline();

    IReadOnlyList<INprPreset> CreateBuiltInPresets()
    {
        return [];
    }
}
