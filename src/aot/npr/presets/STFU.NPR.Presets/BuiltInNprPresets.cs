using STFU.NPR.Composition;

namespace STFU.NPR.Presets;

public static class BuiltInNprPresets
{
    public static IReadOnlyList<INprPreset> CreateAll()
    {
        return
        [
            new TechnicalInkPreset(),
            new PencilConstructionPreset(),
            new PenInkHatchingPreset(),
            new MangaInkPreset(),
            new BlueprintPreset()
        ];
    }
}
