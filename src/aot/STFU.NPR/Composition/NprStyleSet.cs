using STFU.Strokes;

namespace STFU.NPR.Composition;

public sealed record NprStyleSet(
    string Id,
    string Name,
    NprPaper Paper,
    NprRoleStyle Foreground,
    NprRoleStyle Midground,
    NprRoleStyle Background)
{
    public NprRoleStyle GetRoleStyle(NprSceneRole role)
    {
        return role switch
        {
            NprSceneRole.Background => Background,
            NprSceneRole.Midground => Midground,
            _ => Foreground
        };
    }
}

public sealed record NprRoleStyle(
    NprSceneRole Role,
    float OpacityScale,
    float StrokeScale,
    float DetailScale,
    float ToneScale,
    float HatchScale,
    IReadOnlyList<NprLayerStyle> Layers)
{
    public NprLayerStyle PrimaryLayer => Layers.Count == 0
        ? NprLayerStyle.Default(Role)
        : Layers[0];
}

public sealed record NprLayerStyle(
    string Id,
    string Name,
    int Order,
    bool Visible,
    float Opacity,
    NprLayerBlendMode BlendMode,
    NprToneStyle MainFill,
    NprShadingStyle Hatching,
    NprStrokeChannelStyle Contour,
    NprStrokeChannelStyle Crease,
    NprStrokeChannelStyle Accent)
{
    public static NprLayerStyle Default(NprSceneRole role)
    {
        return new NprLayerStyle(
            $"{role.ToString().ToLowerInvariant()}:model",
            $"{role} Model",
            role switch
            {
                NprSceneRole.Background => 10,
                NprSceneRole.Midground => 20,
                _ => 30
            },
            true,
            1f,
            NprLayerBlendMode.Normal,
            new NprToneStyle(true, new StrokeColor(166, 173, 162), 0.18f, 0.42f),
            new NprShadingStyle(true, 0.45f, 1f),
            new NprStrokeChannelStyle(true, 1f, 1f),
            new NprStrokeChannelStyle(true, 0.8f, 0.8f),
            new NprStrokeChannelStyle(true, 0.7f, 0.65f));
    }
}

public sealed record NprToneStyle(
    bool Enabled,
    StrokeColor Color,
    float Opacity,
    float ShadeInfluence);

public sealed record NprShadingStyle(
    bool Enabled,
    float Opacity,
    float DensityScale);

public sealed record NprStrokeChannelStyle(
    bool Enabled,
    float Opacity,
    float ThicknessScale);
