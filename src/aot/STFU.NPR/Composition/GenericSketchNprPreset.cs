using STFU.NPR.Pipeline;
using STFU.NPR.Settings;

namespace STFU.NPR.Composition;

public sealed class GenericSketchNprPreset : INprPreset
{
    public NprPresetMetadata Metadata { get; } = new(
        "generic-sketch",
        "Generic Sketch",
        "Editable built-in NPR sketch preset using feature lines, hatching, density pruning, and approximate hidden-line filtering.",
        true);

    public INprPipeline CreatePipeline()
    {
        return SketchNprPreset.CreatePipeline();
    }

    public NprSettings CreateSettings()
    {
        return SketchNprPreset.CreateSettings();
    }
}
