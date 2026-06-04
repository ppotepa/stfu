using STFU.NPR.Pipeline;

namespace STFU.NPR.Composition;

public sealed class SketchPipelineProvider : INprPipelineProvider
{
    public string PipelineId => NprPipelineIds.Sketch;

    public IReadOnlyList<INprPreset> CreateBuiltInPresets()
    {
        return [new GenericSketchNprPreset()];
    }

    public INprPipeline CreatePipeline()
    {
        return SketchNprPreset.CreatePipeline();
    }
}
