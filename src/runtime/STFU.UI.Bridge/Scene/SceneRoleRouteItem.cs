namespace STFU.UI.Bridge.Scene;

public sealed record SceneRoleRouteItem(
    string Role,
    int LayerCount,
    int VisibleLayerCount,
    float OpacityScale,
    float StrokeScale,
    float DetailScale,
    float ToneScale,
    float HatchScale)
{
    public string Summary =>
        $"{VisibleLayerCount}/{LayerCount} visible, stroke {StrokeScale:0.##}, detail {DetailScale:0.##}, tone {ToneScale:0.##}, hatch {HatchScale:0.##}";
}
