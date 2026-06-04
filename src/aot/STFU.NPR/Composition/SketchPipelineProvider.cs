using STFU.NPR.Pipeline;

namespace STFU.NPR.Composition;

public sealed class SketchPipelineProvider : INprPipelineProvider
{
    public string PipelineId => NprPipelineIds.Sketch;

    public INprPipeline CreatePipeline()
    {
        return SketchNprPreset.CreatePipeline();
    }
}
