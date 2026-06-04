using STFU.NPR.Composition;
using STFU.Strokes;

namespace STFU.NPR.Rendering;

public sealed record NprLayerFrame(
    string Id,
    string Name,
    NprSceneRole Role,
    int Order,
    bool Visible,
    float Opacity,
    NprLayerBlendMode BlendMode,
    IReadOnlyList<NprToneSurface2D> Tones,
    IReadOnlyList<StrokePath2D> Shading,
    IReadOnlyList<StrokePath2D> Strokes);
