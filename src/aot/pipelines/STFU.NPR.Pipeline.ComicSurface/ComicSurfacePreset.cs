using STFU.NPR.Composition;
using STFU.NPR.Graph;
using STFU.NPR.Pipeline;
using STFU.NPR.Settings;
using STFU.Strokes;
using STFU.Strokes.Export;

namespace STFU.NPR.Pipeline.ComicSurface;

public sealed class ComicSurfacePreset : INprPreset
{
    public NprPresetMetadata Metadata { get; } = new(
        "comic-surface",
        "Comic Surface",
        "Surface-first NPR inspired by the prototype: visible face buffer, comic fill, ink contour, and tone-driven hatching.",
        false,
        new Version(1, 0, 0),
        new Version(1, 0, 0),
        "STFU",
        ["comic", "surface", "fill", "built-in"],
        PresetPackaging.BuiltInAot);

    public string PipelineId => NprPipelineIds.ComicSurface;

    public INprPipeline CreatePipeline()
    {
        return ComicSurfacePipeline.Create();
    }

    public NprSettings CreateSettings()
    {
        var settings = SketchNprPreset.CreateSettings();
        settings.Seed = 6601;
        settings.CreaseAngleDegrees = 34f;
        settings.MinimumProjectedTriangleArea = 2f;
        settings.SurfaceBufferScale = 1f;
        settings.MainFillEnabled = true;
        settings.HatchDensity = 0.38f;
        settings.HatchShadeThreshold = 0.42f;
        settings.FeatureLineDensity = 0.9f;
        settings.MinimumSalience = 0.18f;
        settings.HiddenLineDepthBias = 0.0007f;
        settings.StrokeStyle.Medium = StrokeMedium.Marker;
        settings.StrokeStyle.BaseThickness = 1.8f;
        settings.StrokeStyle.ThicknessVariation = 0.22f;
        settings.StrokeStyle.EndpointJitter = 0.22f;
        settings.StrokeStyle.Overshoot = 0.4f;
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
                    FeatureCurveKind.Silhouette or FeatureCurveKind.OccludingContour => rule with { BaseWeight = 1f, MinSalience = 0f, LayerName = "ink-contour" },
                    FeatureCurveKind.Boundary => rule with { BaseWeight = 0.82f, MinSalience = 0f, LayerName = "ink-boundary" },
                    FeatureCurveKind.Crease => rule with { BaseWeight = 0.72f, MinSalience = 0.18f, LayerName = "ink-crease" },
                    FeatureCurveKind.MaterialBoundary => rule with { BaseWeight = 0.68f, MinSalience = 0.16f, LayerName = "ink-material" },
                    FeatureCurveKind.ContactAccent => rule with { BaseWeight = 0.76f, MinSalience = 0.14f, LayerName = "ink-accent" },
                    FeatureCurveKind.Hatch => rule with { BaseWeight = 0.5f, MinSalience = 0.2f, LayerName = "tone-hatch" },
                    FeatureCurveKind.HatchGuide => rule with { Enabled = false },
                    FeatureCurveKind.SurfaceFlow or FeatureCurveKind.Ridge or FeatureCurveKind.Valley or FeatureCurveKind.SuggestiveContour or FeatureCurveKind.ApparentRidge => rule with { Enabled = false },
                    _ => rule
                })
                .ToArray(),
            Visibility = grammar.Visibility with
            {
                Strictness = VisibilityStrictness.Sampled,
                DepthBias = 0.0007f,
                SplitCurves = true,
                DefaultHiddenPolicy = HiddenLinePolicy.Suppress
            },
            Tone = grammar.Tone with
            {
                ToneInfluence = 0.42f,
                ShadeInfluence = 0.72f,
                MinimumOpacity = 0.18f,
                MaximumOpacity = 0.94f
            },
            Hatching = grammar.Hatching with
            {
                Enabled = true,
                ToneThreshold = 0.48f,
                CrossHatchThreshold = 0.72f,
                DeepShadowThreshold = 0.88f,
                DensityScale = 0.78f,
                BaseSpacingPixels = 14f,
                StrokeLengthPixels = 24f,
                JitterRadians = 0.12f,
                UseDirectionField = true
            },
            Stroke = new StyleStrokeRule(
            [
                new StyleStrokeProfile(NprStrokeIntent.Silhouette, 2.35f, 0.98f, new StrokeColor(35, 32, 28), MediumOverride: StrokeMedium.Marker, HumanizationScale: 0.18f),
                new StyleStrokeProfile(NprStrokeIntent.Boundary, 1.55f, 0.86f, new StrokeColor(35, 32, 28), MediumOverride: StrokeMedium.Marker, HumanizationScale: 0.2f),
                new StyleStrokeProfile(NprStrokeIntent.Crease, 1.12f, 0.7f, new StrokeColor(42, 38, 32), MediumOverride: StrokeMedium.Marker, HumanizationScale: 0.22f),
                new StyleStrokeProfile(NprStrokeIntent.Hatch, 0.78f, 0.42f, new StrokeColor(45, 41, 36), MediumOverride: StrokeMedium.Ink, HumanizationScale: 0.2f),
                new StyleStrokeProfile(NprStrokeIntent.Accent, 1.28f, 0.74f, new StrokeColor(30, 28, 25), MediumOverride: StrokeMedium.Marker, HumanizationScale: 0.18f)
            ], 1f, 1f),
            Budget = grammar.Budget with
            {
                MaxSegmentsPerTile = 20,
                AlwaysKeepPrimaryContours = true
            },
            Export = grammar.Export with
            {
                DefaultSvgMode = SvgExportMode.Editable,
                PreferredLayers = ["tone-fill", "tone-hatch-primary", "tone-hatch-cross", "tone-hatch-stipple", "ink-material", "ink-crease", "ink-boundary", "ink-contour", "ink-accent"]
            }
        };
    }

    public NprStyleSet CreateStyleSet()
    {
        return new NprStyleSet(
            Metadata.Id,
            Metadata.Name,
            new NprPaper(new StrokeColor(232, 226, 213), 1f),
            CreateRole(NprSceneRole.Foreground, 30, 1f, 1.12f, 1f, 1f, 1f, 0.82f),
            CreateRole(NprSceneRole.Midground, 20, 0.76f, 0.82f, 0.65f, 0.68f, 0.55f, 0.58f),
            CreateRole(NprSceneRole.Background, 10, 0.48f, 0.58f, 0.35f, 0.42f, 0.24f, 0.38f));
    }

    private static NprRoleStyle CreateRole(
        NprSceneRole role,
        int order,
        float opacity,
        float strokeScale,
        float detailScale,
        float toneScale,
        float hatchScale,
        float fillOpacity)
    {
        var layers = new[]
        {
            ToneFillLayer(order, opacity, fillOpacity),
            ToneHatchLayer("tone-hatch-primary", "Tone Hatch Primary", order + 1, opacity, hatchScale, 0.56f),
            ToneHatchLayer("tone-hatch-cross", "Tone Hatch Cross", order + 2, opacity, hatchScale, 0.46f),
            ToneHatchLayer("tone-hatch-stipple", "Tone Hatch Stipple", order + 3, opacity, hatchScale, 0.38f),
            StrokeLayer("ink-material", $"{role} Material Ink", order + 4, opacity, accent: new NprStrokeChannelStyle(true, 0.72f, 0.76f)),
            StrokeLayer("ink-crease", $"{role} Crease Ink", order + 5, opacity, crease: new NprStrokeChannelStyle(true, 0.78f, 0.82f)),
            StrokeLayer("ink-boundary", $"{role} Boundary Ink", order + 6, opacity, contour: new NprStrokeChannelStyle(true, 0.9f, 0.92f)),
            StrokeLayer("ink-contour", $"{role} Contour Ink", order + 7, opacity, contour: new NprStrokeChannelStyle(true, 1f, 1f)),
            StrokeLayer("ink-accent", $"{role} Accent Ink", order + 8, opacity, accent: new NprStrokeChannelStyle(true, 0.74f, 0.78f))
        };

        return new NprRoleStyle(role, opacity, strokeScale, detailScale, toneScale, hatchScale, layers);
    }

    private static NprLayerStyle ToneFillLayer(int order, float opacity, float fillOpacity)
    {
        return new NprLayerStyle(
            "tone-fill",
            "Tone Fill",
            order,
            true,
            opacity,
            NprLayerBlendMode.Normal,
            new NprToneStyle(true, new StrokeColor(197, 194, 164), fillOpacity, 0.64f),
            DisabledHatching(),
            DisabledStroke(),
            DisabledStroke(),
            DisabledStroke());
    }

    private static NprLayerStyle ToneHatchLayer(string id, string name, int order, float opacity, float hatchScale, float hatchOpacity)
    {
        return new NprLayerStyle(
            id,
            name,
            order,
            true,
            opacity,
            NprLayerBlendMode.Multiply,
            DisabledTone(),
            new NprShadingStyle(true, hatchOpacity, hatchScale),
            DisabledStroke(),
            DisabledStroke(),
            DisabledStroke());
    }

    private static NprLayerStyle StrokeLayer(
        string id,
        string name,
        int order,
        float opacity,
        NprStrokeChannelStyle? contour = null,
        NprStrokeChannelStyle? crease = null,
        NprStrokeChannelStyle? accent = null)
    {
        return new NprLayerStyle(
            id,
            name,
            order,
            true,
            opacity,
            NprLayerBlendMode.Normal,
            DisabledTone(),
            DisabledHatching(),
            contour ?? DisabledStroke(),
            crease ?? DisabledStroke(),
            accent ?? DisabledStroke());
    }

    private static NprToneStyle DisabledTone()
    {
        return new NprToneStyle(false, new StrokeColor(0, 0, 0), 0f, 0f);
    }

    private static NprShadingStyle DisabledHatching()
    {
        return new NprShadingStyle(false, 0f, 0f);
    }

    private static NprStrokeChannelStyle DisabledStroke()
    {
        return new NprStrokeChannelStyle(false, 0f, 0f);
    }
}
