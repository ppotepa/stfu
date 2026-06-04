using System.Text.Json;
using STFU.NPR.Settings;
using STFU.Strokes;

namespace STFU.NPR.Composition;

public sealed class JsonEditableNprPreset : INprPreset
{
    private readonly JsonEditablePresetDocument _document;

    public NprPresetMetadata Metadata { get; }

    public JsonEditableNprPreset(JsonEditablePresetDocument document)
    {
        _document = document;
        var minimumEngineVersion = Version.TryParse(document.Metadata.MinimumEngineVersion, out var parsed)
            ? parsed
            : new Version(1, 0, 0);

        Metadata = new NprPresetMetadata(
            document.Metadata.Id,
            document.Metadata.Name,
            document.Metadata.Description,
            document.Metadata.IsEditable,
            new Version(document.Metadata.PresetVersion.Major, document.Metadata.PresetVersion.Minor, document.Metadata.PresetVersion.Patch),
            minimumEngineVersion,
            document.Metadata.Author,
            document.Metadata.Tags.ToArray(),
            document.Metadata.Packaging)
        {
            PresetVersion = document.Metadata.PresetVersion
        };
    }

    public NprSettings CreateSettings()
    {
        var source = _document.Settings;
        var settings = new NprSettings
        {
            Seed = source.Seed,
            CreaseAngleDegrees = source.CreaseAngleDegrees,
            MinimumProjectedTriangleArea = source.MinimumProjectedTriangleArea,
            MinimumStrokeLength = source.MinimumStrokeLength,
            SurfaceFlowShadeThreshold = source.SurfaceFlowShadeThreshold,
            SurfaceFlowDensity = source.SurfaceFlowDensity,
            HatchShadeThreshold = source.HatchShadeThreshold,
            HatchDensity = source.HatchDensity,
            HatchLength = source.HatchLength,
            HiddenLineDepthBias = source.HiddenLineDepthBias,
            NearClipDepth = source.NearClipDepth,
            FarClipDepth = source.FarClipDepth,
            ScreenClipMarginPixels = source.ScreenClipMarginPixels,
            MaxProjectedTriangleAreaRatio = source.MaxProjectedTriangleAreaRatio,
            FeatureLineDensity = source.FeatureLineDensity,
            MinimumSalience = source.MinimumSalience
        };

        settings.StrokeStyle.Seed = source.StrokeStyle.Seed;
        settings.StrokeStyle.BaseThickness = source.StrokeStyle.BaseThickness;
        settings.StrokeStyle.ThicknessVariation = source.StrokeStyle.ThicknessVariation;
        settings.StrokeStyle.EndpointJitter = source.StrokeStyle.EndpointJitter;
        settings.StrokeStyle.Overshoot = source.StrokeStyle.Overshoot;
        return settings;
    }

    public StyleGrammar CreateGrammar()
    {
        var grammar = _document.Grammar;
        return new StyleGrammar(
            grammar.StyleId,
            grammar.DisplayName,
            new Version(grammar.SchemaVersion.Major, grammar.SchemaVersion.Minor, grammar.SchemaVersion.Patch),
            grammar.FeatureRules.Select(rule => new StyleFeatureRule(
                rule.Kind,
                rule.Enabled,
                rule.BaseWeight,
                rule.MinSalience,
                rule.HiddenLinePolicy,
                rule.Intent,
                rule.LayerOrder,
                rule.LayerName)).ToArray(),
            new StyleVisibilityRule(
                grammar.Visibility.Strictness,
                grammar.Visibility.DepthBias,
                grammar.Visibility.SplitCurves,
                grammar.Visibility.KeepHiddenSegmentsForDebug,
                grammar.Visibility.DefaultHiddenPolicy),
            new StyleToneRule(
                grammar.Tone.Enabled,
                grammar.Tone.ToneInfluence,
                grammar.Tone.ShadeInfluence,
                grammar.Tone.MinimumOpacity,
                grammar.Tone.MaximumOpacity),
            new StyleHatchingRule(
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
            new StyleStrokeRule(
                grammar.Stroke.Profiles.Select(profile => new StyleStrokeProfile(
                    profile.Intent,
                    profile.BaseThickness,
                    profile.BaseOpacity,
                    new StrokeColor(profile.ColorR, profile.ColorG, profile.ColorB))).ToArray(),
                grammar.Stroke.ThicknessScale,
                grammar.Stroke.OpacityScale),
            new StyleBudgetRule(
                grammar.Budget.TileSizePixels,
                grammar.Budget.MaxSegmentsPerTile,
                grammar.Budget.AlwaysKeepPrimaryContours),
            new StyleExportRule(
                grammar.Export.DefaultSvgMode,
                grammar.Export.IncludeMetadata,
                grammar.Export.IncludeDebugLayers,
                grammar.Export.Units,
                grammar.Export.PreferredLayers.ToArray()),
            new StyleDebugRule(grammar.Debug.EnabledOverlays.ToArray()));
    }

    public string ToJson(bool indented = true)
    {
        var options = new JsonSerializerOptions(JsonEditablePresetJsonContext.Default.Options)
        {
            WriteIndented = indented
        };
        return JsonSerializer.Serialize(_document, new JsonEditablePresetJsonContext(options).JsonEditablePresetDocument);
    }

    public static JsonEditableNprPreset FromJson(string json)
    {
        var document = JsonSerializer.Deserialize(json, JsonEditablePresetJsonContext.Default.JsonEditablePresetDocument) as JsonEditablePresetDocument ??
            throw new InvalidOperationException("Preset JSON could not be deserialized.");
        return new JsonEditableNprPreset(document);
    }
}
