using STFU.NPR.Composition;
using STFU.NPR.Graph;
using STFU.NPR.Settings;
using STFU.Strokes;
using STFU.Strokes.Export;

namespace STFU.NPR.Presets;

public sealed class PenInkHatchingPreset : INprPreset
{
    public NprPresetMetadata Metadata { get; } = new(
        "pen-ink-hatching",
        "Pen Ink Hatching",
        "Classic pen-and-ink illustration with dense tone-driven hatch and cross-hatch layers.",
        false,
        new Version(1, 0, 0),
        new Version(1, 0, 0),
        "STFU",
        ["pen", "ink", "hatching", "built-in"],
        PresetPackaging.BuiltInAot);

    public NprSettings CreateSettings()
    {
        var settings = SketchNprPreset.CreateSettings();
        settings.Seed = 3303;
        settings.CreaseAngleDegrees = 32f;
        settings.SurfaceFlowDensity = 0.3f;
        settings.HatchDensity = 0.82f;
        settings.FeatureLineDensity = 0.9f;
        settings.MinimumSalience = 0.22f;
        settings.StrokeStyle.Medium = StrokeMedium.Ink;
        settings.StrokeStyle.BaseThickness = 1.05f;
        settings.StrokeStyle.ThicknessVariation = 0.28f;
        settings.StrokeStyle.EndpointJitter = 0.38f;
        settings.StrokeStyle.Overshoot = 0.85f;
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
                    FeatureCurveKind.Hatch => rule with { BaseWeight = 0.82f, MinSalience = 0.16f, LayerName = "ink-hatch" },
                    FeatureCurveKind.HatchGuide => rule with { BaseWeight = 0.4f, MinSalience = 0.12f, LayerName = "hatch-guide" },
                    FeatureCurveKind.SurfaceFlow => rule with { BaseWeight = 0.34f, MinSalience = 0.24f },
                    FeatureCurveKind.ContactAccent => rule with { BaseWeight = 0.78f, MinSalience = 0.2f },
                    FeatureCurveKind.Construction => rule with { Enabled = false },
                    _ => rule
                })
                .ToArray(),
            Visibility = grammar.Visibility with { DefaultHiddenPolicy = HiddenLinePolicy.KeepForDebug },
            Hatching = grammar.Hatching with
            {
                ToneThreshold = 0.46f,
                CrossHatchThreshold = 0.64f,
                DeepShadowThreshold = 0.78f,
                DensityScale = 1.35f,
                BaseSpacingPixels = 10f,
                StrokeLengthPixels = 22f,
                JitterRadians = 0.18f
            },
            Stroke = new StyleStrokeRule(
            [
                new StyleStrokeProfile(NprStrokeIntent.Silhouette, 1.9f, 0.98f, new StrokeColor(7, 7, 7), HumanizationScale: 0.28f),
                new StyleStrokeProfile(NprStrokeIntent.Boundary, 1.55f, 0.9f, new StrokeColor(12, 12, 12), HumanizationScale: 0.36f),
                new StyleStrokeProfile(NprStrokeIntent.Crease, 1.2f, 0.78f, new StrokeColor(20, 20, 18), HumanizationScale: 0.42f),
                new StyleStrokeProfile(NprStrokeIntent.SurfaceFlow, 0.62f, 0.22f, new StrokeColor(58, 55, 50), HumanizationScale: 0.48f),
                new StyleStrokeProfile(NprStrokeIntent.Hatch, 0.58f, 0.52f, new StrokeColor(26, 25, 23), HumanizationScale: 0.54f, EndpointJitterScale: 0.82f),
                new StyleStrokeProfile(NprStrokeIntent.Hatch, 0.52f, 0.46f, new StrokeColor(32, 31, 29), FeatureCurveKind.Hatch, "ink-hatch-cross", HumanizationScale: 0.42f, EndpointJitterScale: 0.72f),
                new StyleStrokeProfile(NprStrokeIntent.Hatch, 0.46f, 0.38f, new StrokeColor(40, 38, 35), FeatureCurveKind.Hatch, "ink-hatch-tertiary", HumanizationScale: 0.34f, EndpointJitterScale: 0.62f),
                new StyleStrokeProfile(NprStrokeIntent.Accent, 1.05f, 0.7f, new StrokeColor(18, 17, 16), HumanizationScale: 0.38f)
            ], 1f, 1f),
            Budget = grammar.Budget with { MaxSegmentsPerTile = 28, AlwaysKeepPrimaryContours = true },
            Export = grammar.Export with
            {
                PreferredLayers = ["silhouette", "boundary", "crease", "contact-accent", "ink-hatch", "hatch-guide"]
            }
        };
    }
}
