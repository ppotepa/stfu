namespace STFU.NPR.Graph;

public sealed record StyleMask(
    int StableId,
    string Name,
    IReadOnlyList<ScreenPolygon> ScreenRegions,
    float Strength,
    StyleMaskRole Role);
