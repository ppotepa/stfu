using STFU.NPR.Settings;

namespace STFU.NPR.Composition;

public interface INprPreset
{
    NprPresetMetadata Metadata { get; }

    string PipelineId => NprPipelineIds.Default;

    NprSettings CreateSettings();

    StyleGrammar CreateGrammar();

    NprStyleSet CreateStyleSet()
    {
        return SketchNprPreset.CreateStyleSet(Metadata.Id, Metadata.Name);
    }
}
