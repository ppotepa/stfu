using STFU.NPR.Composition;

namespace STFU.NPR.Rendering;

public sealed record NprToneSurface2D(
    string Id,
    string LayerId,
    NprSceneRole Role,
    string Channel,
    int Width,
    int Height,
    byte[] Rgba,
    float Opacity);
