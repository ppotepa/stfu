using STFU.NPR.Pipeline;
using STFU.NPR.Settings;

namespace STFU.NPR.Composition;

public sealed class GenericSketchNprPreset : INprPreset
{
    public NprPresetMetadata Metadata { get; } = new(
        "generic-sketch",
        "Generic Sketch",
        "Editable built-in NPR sketch preset using feature lines, hatching, density pruning, and approximate hidden-line filtering.",
        true,
        new Version(1, 0, 0),
        new Version(1, 0, 0),
        "STFU",
        ["sketch", "npr", "built-in"],
        PresetPackaging.BuiltInAot);

    public INprPipeline CreatePipeline()
    {
        return SketchNprPreset.CreatePipeline();
    }

    public NprSettings CreateSettings()
    {
        return SketchNprPreset.CreateSettings();
    }

    public StyleGrammar CreateGrammar()
    {
        return SketchNprPreset.CreateGrammar();
    }
}
