using STFU.NPR.Composition;
using STFU.NPR.Graph;
using STFU.NPR.Settings;
using STFU.Strokes;
using STFU.Strokes.Export;

namespace STFU.NPR.Presets;

public sealed class BlueprintPreset : INprPreset
{
    public NprPresetMetadata Metadata { get; } = new(
        "blueprint",
        "Blueprint",
        "Layered blueprint-style drawing with visible construction, hidden guides, and pale technical strokes.",
        false,
        new Version(1, 0, 0),
        new Version(1, 0, 0),
        "STFU",
        ["blueprint", "construction", "technical", "built-in"],
        PresetPackaging.BuiltInAot);

    public NprSettings CreateSettings()
    {
        var settings = SketchNprPreset.CreateSettings();
        settings.Seed = 5505;
        settings.CreaseAngleDegrees = 28f;
        settings.SurfaceFlowDensity = 0.22f;
        settings.HatchDensity = 0.18f;
        settings.FeatureLineDensity = 0.98f;
        settings.MinimumSalience = 0.12f;
        settings.StrokeStyle.Medium = StrokeMedium.Marker;
        settings.StrokeStyle.BaseThickness = 0.95f;
        settings.StrokeStyle.ThicknessVariation = 0.05f;
        settings.StrokeStyle.EndpointJitter = 0.04f;
        settings.StrokeStyle.Overshoot = 0.1f;
        return settings;
    }

    public StyleGrammar CreateGrammar()
    {
        var grammar = SketchNprPreset.CreateGrammar();
        return grammar with
        {
            StyleId = Metadata.Id,
            DisplayName = Metadata.Name,
            FeatureRules = grammar.FeatureRules
                .Select(rule => rule.Kind switch
                {
                    FeatureCurveKind.Construction => rule with { Enabled = true, HiddenLinePolicy = HiddenLinePolicy.Ghost, BaseWeight = 0.68f, MinSalience = 0.04f, LayerName = "construction" },
                    FeatureCurveKind.HatchGuide => rule with { Enabled = true, BaseWeight = 0.42f, MinSalience = 0.08f, LayerName = "hatch-guide" },
                    FeatureCurveKind.Hatch => rule with { BaseWeight = 0.28f, MinSalience = 0.26f, LayerName = "light-hatch" },
                    FeatureCurveKind.SurfaceFlow => rule with { BaseWeight = 0.38f, MinSalience = 0.22f, LayerName = "form-guide" },
                    FeatureCurveKind.ContactAccent => rule with { BaseWeight = 0.36f, MinSalience = 0.24f, LayerName = "contact" },
                    _ => rule
                })
                .ToArray(),
            Visibility = grammar.Visibility with
            {
                DefaultHiddenPolicy = HiddenLinePolicy.Ghost,
                KeepHiddenSegmentsForDebug = true
            },
            Hatching = grammar.Hatching with
            {
                ToneThreshold = 0.72f,
                CrossHatchThreshold = 0.9f,
                DeepShadowThreshold = 0.98f,
                DensityScale = 0.32f,
                BaseSpacingPixels = 22f,
                StrokeLengthPixels = 28f,
                JitterRadians = 0.03f
            },
            Stroke = new StyleStrokeRule(
            [
                new StyleStrokeProfile(NprStrokeIntent.Silhouette, 1.35f, 0.9f, new StrokeColor(218, 246, 255), MediumOverride: StrokeMedium.Marker, HumanizationScale: 0.08f),
                new StyleStrokeProfile(NprStrokeIntent.Boundary, 1.1f, 0.78f, new StrokeColor(194, 232, 246), MediumOverride: StrokeMedium.Marker, HumanizationScale: 0.08f),
                new StyleStrokeProfile(NprStrokeIntent.Crease, 0.85f, 0.65f, new StrokeColor(175, 220, 240), MediumOverride: StrokeMedium.Marker, HumanizationScale: 0.1f),
                new StyleStrokeProfile(NprStrokeIntent.SurfaceFlow, 0.55f, 0.28f, new StrokeColor(134, 190, 218), MediumOverride: StrokeMedium.Marker, HumanizationScale: 0.08f),
                new StyleStrokeProfile(NprStrokeIntent.Hatch, 0.45f, 0.24f, new StrokeColor(122, 182, 212), MediumOverride: StrokeMedium.Marker, HumanizationScale: 0.08f),
                new StyleStrokeProfile(NprStrokeIntent.Accent, 0.72f, 0.5f, new StrokeColor(190, 235, 250), MediumOverride: StrokeMedium.Marker, HumanizationScale: 0.08f)
            ], 1f, 0.85f),
            Budget = grammar.Budget with { MaxSegmentsPerTile = 24, AlwaysKeepPrimaryContours = true },
            Export = grammar.Export with
            {
                DefaultSvgMode = SvgExportMode.Editable,
                PreferredLayers = ["visible-contour", "boundary", "crease", "construction", "form-guide", "hatch-guide", "light-hatch"]
            }
        };
    }
}
