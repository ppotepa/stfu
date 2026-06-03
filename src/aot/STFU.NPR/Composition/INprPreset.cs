using STFU.NPR.Pipeline;
using STFU.NPR.Settings;

namespace STFU.NPR.Composition;

public interface INprPreset
{
    NprPresetMetadata Metadata { get; }

    INprPipeline CreatePipeline();

    NprSettings CreateSettings();
}
