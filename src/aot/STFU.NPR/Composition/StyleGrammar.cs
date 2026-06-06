using STFU.Common.Math;
using STFU.NPR.Graph;
using STFU.Strokes.Export;

namespace STFU.NPR.Composition;

public sealed record StyleGrammar(
    string StyleId,
    string DisplayName,
    Version SchemaVersion,
    IReadOnlyList<StyleFeatureRule> FeatureRules,
    StyleVisibilityRule Visibility,
    StyleToneRule Tone,
    StyleHatchingRule Hatching,
    StyleStrokeRule Stroke,
    StyleBudgetRule Budget,
    StyleExportRule Export,
    StyleDebugRule Debug)
{
    public StyleFeatureRule? FindFeatureRule(FeatureCurveKind kind)
    {
        return FeatureRules.FirstOrDefault(rule => rule.Kind == kind);
    }

    public StyleFeatureRule? FindFeatureRule(NprStrokeIntent intent)
    {
        return FeatureRules.FirstOrDefault(rule => rule.Intent == intent);
    }

    public float GetMinimumSalience(FeatureCurveKind kind, NprStrokeIntent intent, float fallback)
    {
        return FindFeatureRule(kind)?.MinSalience ??
            FindFeatureRule(intent)?.MinSalience ??
            fallback;
    }

    public float GetMinimumSalience(NprStrokeIntent intent, float fallback)
    {
        return FindFeatureRule(intent)?.MinSalience ?? fallback;
    }

    public float GetBaseWeight(FeatureCurveKind kind, NprStrokeIntent intent, float fallback)
    {
        return FindFeatureRule(kind)?.BaseWeight ??
            FindFeatureRule(intent)?.BaseWeight ??
            fallback;
    }

    public float GetBaseWeight(NprStrokeIntent intent, float fallback)
    {
        return FindFeatureRule(intent)?.BaseWeight ?? fallback;
    }

    public string GetLayerName(FeatureCurveKind kind, NprStrokeIntent intent)
    {
        return FindFeatureRule(kind)?.LayerName ??
            FindFeatureRule(intent)?.LayerName ??
            intent.ToString();
    }

    public string GetLayerName(NprStrokeIntent intent)
    {
        return FindFeatureRule(intent)?.LayerName ?? intent.ToString();
    }

    public string ResolveOutputLayer(
        FeatureCurveKind kind,
        NprStrokeIntent intent,
        VisibilityState visibility,
        HatchLayerKind? hatchLayerKind = null)
    {
        var layer = GetLayerName(kind, intent);
        if (visibility == VisibilityState.Hidden &&
            string.IsNullOrWhiteSpace(layer) &&
            GetHiddenLinePolicy(kind, intent) is HiddenLinePolicy.Dashed or HiddenLinePolicy.Ghost)
        {
            return NprLayerIds.Hidden;
        }

        if (kind == FeatureCurveKind.Hatch && hatchLayerKind is not null)
        {
            return $"{layer}-{ToHatchLayerSuffix(hatchLayerKind.Value)}";
        }

        return layer;
    }

    public int GetLayerOrder(
        FeatureCurveKind kind,
        NprStrokeIntent intent,
        VisibilityState visibility,
        HatchLayerKind? hatchLayerKind = null)
    {
        var order = FindFeatureRule(kind)?.LayerOrder ??
            FindFeatureRule(intent)?.LayerOrder ??
            100;

        if (visibility == VisibilityState.Hidden)
        {
            order += 8;
        }

        if (kind == FeatureCurveKind.Hatch && hatchLayerKind is not null)
        {
            order += hatchLayerKind.Value switch
            {
                HatchLayerKind.Tertiary => 2,
                HatchLayerKind.Cross => 1,
                _ => 0
            };
        }

        return order;
    }

    public bool IsEnabled(FeatureCurveKind kind, NprStrokeIntent intent)
    {
        return FindFeatureRule(kind)?.Enabled ??
            FindFeatureRule(intent)?.Enabled ??
            true;
    }

    public bool IsEnabled(NprStrokeIntent intent)
    {
        return FindFeatureRule(intent)?.Enabled ?? true;
    }

    public HiddenLinePolicy GetHiddenLinePolicy(FeatureCurveKind kind, NprStrokeIntent intent)
    {
        return FindFeatureRule(kind)?.HiddenLinePolicy ??
            FindFeatureRule(intent)?.HiddenLinePolicy ??
            Visibility.DefaultHiddenPolicy;
    }

    public LinePriorityRule BuildPriorityRule(FeatureCurveKind kind, NprStrokeIntent intent, float fallbackMinScreenLength, float fallbackMaxDensityPerTile)
    {
        var featureRule = FindFeatureRule(kind) ?? FindFeatureRule(intent);
        var minScreenLength = kind switch
        {
            FeatureCurveKind.Boundary or FeatureCurveKind.Silhouette => 0f,
            FeatureCurveKind.ContactAccent => NumericMath.AtLeast(fallbackMinScreenLength * 0.75f, 0f),
            FeatureCurveKind.Construction => NumericMath.AtLeast(fallbackMinScreenLength * 0.6f, 0f),
            FeatureCurveKind.HatchGuide => NumericMath.AtLeast(fallbackMinScreenLength * 0.55f, 0f),
            FeatureCurveKind.Hatch => NumericMath.AtLeast(fallbackMinScreenLength * 0.65f, 0f),
            FeatureCurveKind.SurfaceFlow => NumericMath.AtLeast(fallbackMinScreenLength * 0.85f, 0f),
            FeatureCurveKind.Ridge or FeatureCurveKind.Valley or FeatureCurveKind.SuggestiveContour or FeatureCurveKind.ApparentRidge => NumericMath.AtLeast(fallbackMinScreenLength * 0.9f, 0f),
            _ => NumericMath.AtLeast(fallbackMinScreenLength, 0f)
        };

        var densityPerTile = kind switch
        {
            FeatureCurveKind.ContactAccent => NumericMath.AtLeast(fallbackMaxDensityPerTile * 0.65f, 1f),
            FeatureCurveKind.Construction => NumericMath.AtLeast(fallbackMaxDensityPerTile * 0.9f, 1f),
            FeatureCurveKind.HatchGuide => NumericMath.AtLeast(fallbackMaxDensityPerTile * 0.85f, 1f),
            FeatureCurveKind.Hatch => NumericMath.AtLeast(fallbackMaxDensityPerTile, 1f),
            FeatureCurveKind.SurfaceFlow => NumericMath.AtLeast(fallbackMaxDensityPerTile * 0.8f, 1f),
            FeatureCurveKind.Ridge or FeatureCurveKind.Valley or FeatureCurveKind.SuggestiveContour or FeatureCurveKind.ApparentRidge => NumericMath.AtLeast(fallbackMaxDensityPerTile * 0.7f, 1f),
            _ => NumericMath.AtLeast(fallbackMaxDensityPerTile * 0.75f, 1f)
        };

        return new LinePriorityRule(
            kind,
            featureRule?.BaseWeight ?? 1f,
            minScreenLength,
            densityPerTile,
            GetHiddenLinePolicy(kind, intent),
            kind is FeatureCurveKind.Boundary or FeatureCurveKind.Silhouette);
    }

    public SvgExportOptions CreateSvgExportOptions(float scale = 1f)
    {
        return new SvgExportOptions(
            Export.DefaultSvgMode,
            Export.IncludeMetadata,
            Export.IncludeDebugLayers,
            scale,
            Export.Units,
            ExpandPreferredLayers(Export.PreferredLayers));
    }

    private static IReadOnlyList<string> ExpandPreferredLayers(IReadOnlyList<string> layers)
    {
        if (layers.Count == 0)
        {
            return layers;
        }

        var expanded = new List<string>(layers.Count * 2);
        foreach (var layer in layers)
        {
            AddUnique(expanded, layer);
            if (ShouldExpandHatchLayer(layer))
            {
                AddUnique(expanded, $"{layer}-tertiary");
                AddUnique(expanded, $"{layer}-cross");
                AddUnique(expanded, $"{layer}-primary");
            }
        }

        return expanded;
    }

    private static bool ShouldExpandHatchLayer(string layer)
    {
        return layer.Contains("hatch", StringComparison.OrdinalIgnoreCase) ||
            layer.Contains("tone-mark", StringComparison.OrdinalIgnoreCase);
    }

    private static void AddUnique(List<string> layers, string layer)
    {
        if (!layers.Any(existing => string.Equals(existing, layer, StringComparison.OrdinalIgnoreCase)))
        {
            layers.Add(layer);
        }
    }

    private static string ToHatchLayerSuffix(HatchLayerKind kind)
    {
        return kind switch
        {
            HatchLayerKind.Cross => "cross",
            HatchLayerKind.Tertiary => "tertiary",
            HatchLayerKind.Contour => "contour",
            HatchLayerKind.Screentone => "screentone",
            HatchLayerKind.Stipple => "stipple",
            _ => "primary"
        };
    }
}
