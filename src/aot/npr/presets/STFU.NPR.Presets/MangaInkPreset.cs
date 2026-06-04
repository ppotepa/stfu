using STFU.NPR.Composition;
using STFU.NPR.Graph;
using STFU.NPR.Settings;
using STFU.Strokes;
using STFU.Strokes.Export;

namespace STFU.NPR.Presets;

public sealed class MangaInkPreset : INprPreset
{
    public NprPresetMetadata Metadata { get; } = new(
        "manga-ink",
        "Manga Ink",
        "High-contrast comic line art with strong contours, accents, and restrained directional tone marks.",
        false,
        new Version(1, 0, 0),
        new Version(1, 0, 0),
        "STFU",
        ["manga", "comic", "ink", "built-in"],
        PresetPackaging.BuiltInAot);

    public NprSettings CreateSettings()
    {
        var settings = SketchNprPreset.CreateSettings();
        settings.Seed = 4404;
        settings.CreaseAngleDegrees = 30f;
        settings.SurfaceFlowDensity = 0.16f;
        settings.HatchDensity = 0.42f;
        settings.FeatureLineDensity = 0.94f;
        settings.MinimumSalience = 0.24f;
        settings.StrokeStyle.Medium = StrokeMedium.Marker;
        settings.StrokeStyle.BaseThickness = 1.55f;
        settings.StrokeStyle.ThicknessVariation = 0.2f;
        settings.StrokeStyle.EndpointJitter = 0.12f;
        settings.StrokeStyle.Overshoot = 0.35f;
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
                    FeatureCurveKind.Silhouette or FeatureCurveKind.OccludingContour => rule with { BaseWeight = 1f, MinSalience = 0f, LayerName = "hero-contour" },
                    FeatureCurveKind.ContactAccent => rule with { BaseWeight = 0.94f, MinSalience = 0.12f, LayerName = "black-accent" },
                    FeatureCurveKind.Crease => rule with { BaseWeight = 0.82f, MinSalience = 0.28f },
                    FeatureCurveKind.Hatch => rule with { BaseWeight = 0.52f, MinSalience = 0.32f, LayerName = "tone-mark" },
                    FeatureCurveKind.HatchGuide or FeatureCurveKind.Construction => rule with { Enabled = false },
                    FeatureCurveKind.SurfaceFlow => rule with { BaseWeight = 0.2f, MinSalience = 0.48f },
                    _ => rule
                })
                .ToArray(),
            Visibility = grammar.Visibility with { DefaultHiddenPolicy = HiddenLinePolicy.Suppress },
            Hatching = grammar.Hatching with
            {
                ToneThreshold = 0.7f,
                CrossHatchThreshold = 0.86f,
                DeepShadowThreshold = 0.94f,
                DensityScale = 0.72f,
                BaseSpacingPixels = 13f,
                StrokeLengthPixels = 26f,
                JitterRadians = 0.08f
            },
            Stroke = new StyleStrokeRule(
            [
                new StyleStrokeProfile(NprStrokeIntent.Silhouette, 2.65f, 1f, new StrokeColor(0, 0, 0), MediumOverride: StrokeMedium.Marker, HumanizationScale: 0.08f),
                new StyleStrokeProfile(NprStrokeIntent.Boundary, 2f, 0.95f, new StrokeColor(0, 0, 0), MediumOverride: StrokeMedium.Marker, HumanizationScale: 0.1f),
                new StyleStrokeProfile(NprStrokeIntent.Crease, 1.35f, 0.78f, new StrokeColor(8, 8, 8), MediumOverride: StrokeMedium.Marker, HumanizationScale: 0.14f),
                new StyleStrokeProfile(NprStrokeIntent.SurfaceFlow, 0.55f, 0.16f, new StrokeColor(55, 55, 55), MediumOverride: StrokeMedium.Marker, HumanizationScale: 0.16f),
                new StyleStrokeProfile(NprStrokeIntent.Hatch, 0.78f, 0.5f, new StrokeColor(8, 8, 8), MediumOverride: StrokeMedium.Marker, HumanizationScale: 0.12f),
                new StyleStrokeProfile(NprStrokeIntent.Accent, 1.55f, 0.86f, new StrokeColor(0, 0, 0), MediumOverride: StrokeMedium.Marker, HumanizationScale: 0.08f)
            ], 1.05f, 1f),
            Budget = grammar.Budget with { MaxSegmentsPerTile = 16, AlwaysKeepPrimaryContours = true },
            Export = grammar.Export with
            {
                PreferredLayers = ["hero-contour", "boundary", "crease", "black-accent", "tone-mark"]
            }
        };
    }
}
