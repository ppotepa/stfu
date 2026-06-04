using STFU.NPR.Composition;
using STFU.NPR.Graph;
using STFU.NPR.Settings;
using STFU.Strokes;
using STFU.Strokes.Export;

namespace STFU.NPR.Presets;

public sealed class PencilConstructionPreset : INprPreset
{
    public NprPresetMetadata Metadata { get; } = new(
        "pencil-construction",
        "Pencil Construction",
        "Loose graphite construction sketch with ghosted hidden guides, visible form flow, and soft pressure variation.",
        false,
        new Version(1, 0, 0),
        new Version(1, 0, 0),
        "STFU",
        ["pencil", "construction", "sketch", "built-in"],
        PresetPackaging.BuiltInAot);

    public NprSettings CreateSettings()
    {
        var settings = SketchNprPreset.CreateSettings();
        settings.Seed = 2202;
        settings.CreaseAngleDegrees = 38f;
        settings.SurfaceFlowDensity = 0.62f;
        settings.HatchDensity = 0.34f;
        settings.FeatureLineDensity = 0.78f;
        settings.MinimumSalience = 0.16f;
        settings.StrokeStyle.Medium = StrokeMedium.Pencil;
        settings.StrokeStyle.BaseThickness = 1.12f;
        settings.StrokeStyle.ThicknessVariation = 0.68f;
        settings.StrokeStyle.EndpointJitter = 1.45f;
        settings.StrokeStyle.Overshoot = 2.7f;
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
                    FeatureCurveKind.Construction => rule with { Enabled = true, HiddenLinePolicy = HiddenLinePolicy.Ghost, BaseWeight = 0.52f, MinSalience = 0.08f, LayerName = "construction" },
                    FeatureCurveKind.HatchGuide => rule with { Enabled = true, BaseWeight = 0.34f, MinSalience = 0.08f, LayerName = "guide" },
                    FeatureCurveKind.SurfaceFlow => rule with { Enabled = true, BaseWeight = 0.62f, MinSalience = 0.12f, LayerName = "surface-flow" },
                    FeatureCurveKind.SuggestiveContour or FeatureCurveKind.ApparentRidge => rule with { BaseWeight = 0.86f, MinSalience = 0.2f },
                    FeatureCurveKind.Hatch => rule with { BaseWeight = 0.42f, MinSalience = 0.2f },
                    _ => rule
                })
                .ToArray(),
            Visibility = grammar.Visibility with { DefaultHiddenPolicy = HiddenLinePolicy.Ghost },
            Hatching = grammar.Hatching with
            {
                ToneThreshold = 0.66f,
                CrossHatchThreshold = 0.86f,
                DeepShadowThreshold = 0.95f,
                DensityScale = 0.55f,
                BaseSpacingPixels = 18f,
                StrokeLengthPixels = 24f,
                JitterRadians = 0.46f
            },
            Stroke = new StyleStrokeRule(
            [
                new StyleStrokeProfile(NprStrokeIntent.Silhouette, 1.65f, 0.78f, new StrokeColor(34, 32, 30), MediumOverride: StrokeMedium.Pencil, HumanizationScale: 0.72f),
                new StyleStrokeProfile(NprStrokeIntent.Boundary, 1.35f, 0.62f, new StrokeColor(48, 45, 42), MediumOverride: StrokeMedium.Pencil, HumanizationScale: 1f),
                new StyleStrokeProfile(NprStrokeIntent.Crease, 1.05f, 0.5f, new StrokeColor(58, 55, 52), MediumOverride: StrokeMedium.Pencil, HumanizationScale: 1.05f),
                new StyleStrokeProfile(NprStrokeIntent.SurfaceFlow, 0.72f, 0.28f, new StrokeColor(82, 78, 72), MediumOverride: StrokeMedium.Pencil, HumanizationScale: 1.28f, EndpointJitterScale: 1.15f),
                new StyleStrokeProfile(NprStrokeIntent.Hatch, 0.55f, 0.25f, new StrokeColor(70, 66, 62), MediumOverride: StrokeMedium.Pencil, HumanizationScale: 1.18f, ThicknessVariationScale: 1.2f),
                new StyleStrokeProfile(NprStrokeIntent.Accent, 0.92f, 0.4f, new StrokeColor(52, 48, 45), MediumOverride: StrokeMedium.Pencil, HumanizationScale: 1.08f)
            ], 1f, 0.92f),
            Budget = grammar.Budget with { MaxSegmentsPerTile = 22, AlwaysKeepPrimaryContours = true },
            Export = grammar.Export with
            {
                PreferredLayers = ["silhouette", "boundary", "construction", "surface-flow", "guide", "hatch"]
            }
        };
    }
}
