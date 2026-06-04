using STFU.NPR.Debug;
using STFU.NPR.Graph;
using STFU.NPR.Settings;
using STFU.NPR.Styles;
using STFU.Strokes;
using STFU.Strokes.Export;

namespace STFU.NPR.Composition;

public sealed record JsonEditablePresetDocument(
    JsonEditablePresetMetadata Metadata,
    JsonEditablePresetSettings Settings,
    JsonEditableStyleGrammar Grammar)
{
    public static JsonEditablePresetDocument FromPreset(INprPreset preset)
    {
        var metadata = preset.Metadata;
        var settings = preset.CreateSettings();
        var grammar = preset.CreateGrammar();

        return new JsonEditablePresetDocument(
            new JsonEditablePresetMetadata(
                metadata.Id,
                metadata.Name,
                metadata.Description,
                metadata.IsEditable,
                metadata.PresetVersion,
                metadata.MinimumEngineVersion.ToString(),
                metadata.Author,
                metadata.Tags.ToArray(),
                metadata.Packaging),
            new JsonEditablePresetSettings(
                settings.Seed,
                settings.CreaseAngleDegrees,
                settings.MinimumProjectedTriangleArea,
                settings.MinimumStrokeLength,
                settings.SurfaceFlowShadeThreshold,
                settings.SurfaceFlowDensity,
                settings.HatchShadeThreshold,
                settings.HatchDensity,
                settings.HatchLength,
                settings.HiddenLineDepthBias,
                settings.NearClipDepth,
                settings.FarClipDepth,
                settings.ScreenClipMarginPixels,
                settings.MaxProjectedTriangleAreaRatio,
                settings.FeatureLineDensity,
                settings.MinimumSalience,
                new JsonEditableStrokeStyle(
                    settings.StrokeStyle.Seed,
                    settings.StrokeStyle.BaseThickness,
                    settings.StrokeStyle.ThicknessVariation,
                    settings.StrokeStyle.EndpointJitter,
                    settings.StrokeStyle.Overshoot)),
            new JsonEditableStyleGrammar(
                grammar.StyleId,
                grammar.DisplayName,
                new PresetVersion(grammar.SchemaVersion.Major, grammar.SchemaVersion.Minor, grammar.SchemaVersion.Build < 0 ? 0 : grammar.SchemaVersion.Build),
                grammar.FeatureRules.Select(rule => new JsonEditableStyleFeatureRule(
                    rule.Kind,
                    rule.Enabled,
                    rule.BaseWeight,
                    rule.MinSalience,
                    rule.HiddenLinePolicy,
                    rule.Intent,
                    rule.LayerOrder,
                    rule.LayerName)).ToArray(),
                new JsonEditableStyleVisibilityRule(
                    grammar.Visibility.Strictness,
                    grammar.Visibility.DepthBias,
                    grammar.Visibility.SplitCurves,
                    grammar.Visibility.KeepHiddenSegmentsForDebug,
                    grammar.Visibility.DefaultHiddenPolicy),
                new JsonEditableStyleToneRule(
                    grammar.Tone.Enabled,
                    grammar.Tone.ToneInfluence,
                    grammar.Tone.ShadeInfluence,
                    grammar.Tone.MinimumOpacity,
                    grammar.Tone.MaximumOpacity),
                new JsonEditableStyleHatchingRule(
                    grammar.Hatching.Enabled,
                    grammar.Hatching.ToneThreshold,
                    grammar.Hatching.CrossHatchThreshold,
                    grammar.Hatching.DeepShadowThreshold,
                    grammar.Hatching.DensityScale,
                    grammar.Hatching.BaseSpacingPixels,
                    grammar.Hatching.StrokeLengthPixels,
                    grammar.Hatching.DirectionAngleOffsetRadians,
                    grammar.Hatching.CrossAngleOffsetRadians,
                    grammar.Hatching.TertiaryAngleOffsetRadians,
                    grammar.Hatching.JitterRadians,
                    grammar.Hatching.UseDirectionField),
                new JsonEditableStyleStrokeRule(
                    grammar.Stroke.Profiles.Select(profile => new JsonEditableStyleStrokeProfile(
                        profile.Intent,
                        profile.BaseThickness,
                        profile.BaseOpacity,
                        profile.Color.R,
                        profile.Color.G,
                        profile.Color.B)).ToArray(),
                    grammar.Stroke.ThicknessScale,
                    grammar.Stroke.OpacityScale),
                new JsonEditableStyleBudgetRule(
                    grammar.Budget.TileSizePixels,
                    grammar.Budget.MaxSegmentsPerTile,
                    grammar.Budget.AlwaysKeepPrimaryContours),
                new JsonEditableStyleExportRule(
                    grammar.Export.DefaultSvgMode,
                    grammar.Export.IncludeMetadata,
                    grammar.Export.IncludeDebugLayers,
                    grammar.Export.Units,
                    grammar.Export.PreferredLayers.ToArray()),
                new JsonEditableStyleDebugRule(grammar.Debug.EnabledOverlays.ToArray())));
    }
}

public sealed record JsonEditablePresetMetadata(
    string Id,
    string Name,
    string Description,
    bool IsEditable,
    PresetVersion PresetVersion,
    string MinimumEngineVersion,
    string Author,
    IReadOnlyList<string> Tags,
    PresetPackaging Packaging);

public sealed record JsonEditablePresetSettings(
    int Seed,
    float CreaseAngleDegrees,
    float MinimumProjectedTriangleArea,
    float MinimumStrokeLength,
    float SurfaceFlowShadeThreshold,
    float SurfaceFlowDensity,
    float HatchShadeThreshold,
    float HatchDensity,
    float HatchLength,
    float HiddenLineDepthBias,
    float NearClipDepth,
    float FarClipDepth,
    float ScreenClipMarginPixels,
    float MaxProjectedTriangleAreaRatio,
    float FeatureLineDensity,
    float MinimumSalience,
    JsonEditableStrokeStyle StrokeStyle);

public sealed record JsonEditableStrokeStyle(
    int Seed,
    float BaseThickness,
    float ThicknessVariation,
    float EndpointJitter,
    float Overshoot);

public sealed record JsonEditableStyleGrammar(
    string StyleId,
    string DisplayName,
    PresetVersion SchemaVersion,
    IReadOnlyList<JsonEditableStyleFeatureRule> FeatureRules,
    JsonEditableStyleVisibilityRule Visibility,
    JsonEditableStyleToneRule Tone,
    JsonEditableStyleHatchingRule Hatching,
    JsonEditableStyleStrokeRule Stroke,
    JsonEditableStyleBudgetRule Budget,
    JsonEditableStyleExportRule Export,
    JsonEditableStyleDebugRule Debug);

public sealed record JsonEditableStyleFeatureRule(
    FeatureCurveKind Kind,
    bool Enabled,
    float BaseWeight,
    float MinSalience,
    HiddenLinePolicy HiddenLinePolicy,
    NprStrokeIntent Intent,
    int LayerOrder,
    string LayerName);

public sealed record JsonEditableStyleVisibilityRule(
    VisibilityStrictness Strictness,
    float DepthBias,
    bool SplitCurves,
    bool KeepHiddenSegmentsForDebug,
    HiddenLinePolicy DefaultHiddenPolicy);

public sealed record JsonEditableStyleToneRule(
    bool Enabled,
    float ToneInfluence,
    float ShadeInfluence,
    float MinimumOpacity,
    float MaximumOpacity);

public sealed record JsonEditableStyleHatchingRule(
    bool Enabled,
    float ToneThreshold,
    float CrossHatchThreshold,
    float DeepShadowThreshold,
    float DensityScale,
    float BaseSpacingPixels,
    float StrokeLengthPixels,
    float DirectionAngleOffsetRadians,
    float CrossAngleOffsetRadians,
    float TertiaryAngleOffsetRadians,
    float JitterRadians,
    bool UseDirectionField);

public sealed record JsonEditableStyleStrokeRule(
    IReadOnlyList<JsonEditableStyleStrokeProfile> Profiles,
    float ThicknessScale,
    float OpacityScale);

public sealed record JsonEditableStyleStrokeProfile(
    NprStrokeIntent Intent,
    float BaseThickness,
    float BaseOpacity,
    byte ColorR,
    byte ColorG,
    byte ColorB);

public sealed record JsonEditableStyleBudgetRule(
    int TileSizePixels,
    int MaxSegmentsPerTile,
    bool AlwaysKeepPrimaryContours);

public sealed record JsonEditableStyleExportRule(
    SvgExportMode DefaultSvgMode,
    bool IncludeMetadata,
    bool IncludeDebugLayers,
    string Units,
    IReadOnlyList<string> PreferredLayers);

public sealed record JsonEditableStyleDebugRule(
    IReadOnlyList<DebugOverlayKind> EnabledOverlays);
