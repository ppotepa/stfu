using STFU.NPR.Composition;
using STFU.NPR.Graph;
using STFU.NPR.Settings;
using STFU.Strokes;
using STFU.Strokes.Export;

namespace STFU.NPR.Presets;

public sealed class TechnicalInkPreset : INprPreset
{
    public NprPresetMetadata Metadata { get; } = new(
        "technical-ink",
        "Technical Ink",
        "Clean technical line drawing with strict contours, creases, material boundaries, and minimal hatching.",
        false,
        new Version(1, 0, 0),
        new Version(1, 0, 0),
        "STFU",
        ["technical", "ink", "svg", "built-in"],
        PresetPackaging.BuiltInAot);

    public NprSettings CreateSettings()
    {
        var settings = SketchNprPreset.CreateSettings();
        settings.Seed = 1101;
        settings.CreaseAngleDegrees = 26f;
        settings.SurfaceFlowDensity = 0.05f;
        settings.HatchDensity = 0.02f;
        settings.FeatureLineDensity = 0.96f;
        settings.MinimumSalience = 0.18f;
        settings.StrokeStyle.Medium = StrokeMedium.Ink;
        settings.StrokeStyle.BaseThickness = 1.05f;
        settings.StrokeStyle.ThicknessVariation = 0.08f;
        settings.StrokeStyle.EndpointJitter = 0.08f;
        settings.StrokeStyle.Overshoot = 0.2f;
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
                    FeatureCurveKind.Boundary => rule with { BaseWeight = 1f, MinSalience = 0f, LayerName = "boundary" },
                    FeatureCurveKind.Silhouette or FeatureCurveKind.OccludingContour => rule with { BaseWeight = 1f, MinSalience = 0f, LayerName = "visible-contour" },
                    FeatureCurveKind.Crease => rule with { BaseWeight = 0.92f, MinSalience = 0.18f, LayerName = "crease" },
                    FeatureCurveKind.MaterialBoundary => rule with { BaseWeight = 0.86f, MinSalience = 0.2f, LayerName = "material-boundary" },
                    FeatureCurveKind.Hatch or FeatureCurveKind.HatchGuide or FeatureCurveKind.SurfaceFlow => rule with { Enabled = false },
                    FeatureCurveKind.Construction => rule with { HiddenLinePolicy = HiddenLinePolicy.Dashed, BaseWeight = 0.32f, LayerName = "hidden" },
                    _ => rule with { BaseWeight = 0.44f, MinSalience = 0.42f }
                })
                .ToArray(),
            Visibility = grammar.Visibility with
            {
                DefaultHiddenPolicy = HiddenLinePolicy.Dashed,
                KeepHiddenSegmentsForDebug = true
            },
            Hatching = grammar.Hatching with
            {
                Enabled = false,
                DensityScale = 0f
            },
            Stroke = new StyleStrokeRule(
            [
                new StyleStrokeProfile(NprStrokeIntent.Silhouette, 1.55f, 0.98f, new StrokeColor(8, 8, 8), HumanizationScale: 0.12f),
                new StyleStrokeProfile(NprStrokeIntent.Boundary, 1.25f, 0.92f, new StrokeColor(16, 16, 16), HumanizationScale: 0.14f),
                new StyleStrokeProfile(NprStrokeIntent.Crease, 0.95f, 0.78f, new StrokeColor(24, 24, 24), HumanizationScale: 0.18f),
                new StyleStrokeProfile(NprStrokeIntent.SurfaceFlow, 0.5f, 0.16f, new StrokeColor(90, 90, 90), HumanizationScale: 0.2f),
                new StyleStrokeProfile(NprStrokeIntent.Hatch, 0.45f, 0.12f, new StrokeColor(90, 90, 90), HumanizationScale: 0.2f),
                new StyleStrokeProfile(NprStrokeIntent.Accent, 0.85f, 0.56f, new StrokeColor(30, 30, 30), HumanizationScale: 0.16f)
            ], 1f, 1f),
            Budget = grammar.Budget with { MaxSegmentsPerTile = 18, AlwaysKeepPrimaryContours = true },
            Export = grammar.Export with
            {
                DefaultSvgMode = SvgExportMode.Editable,
                PreferredLayers = ["visible-contour", "boundary", "crease", "material-boundary", "hidden"]
            }
        };
    }
}
