using STFU.NPR.Composition;
using STFU.NPR.Graph;
using STFU.NPR.Settings;
using STFU.Strokes;
using STFU.Strokes.Export;

namespace STFU.NPR.Pipeline.ReferenceQuality;

public sealed class DefaultNprPreset : INprPreset
{
    public NprPresetMetadata Metadata { get; } = new(
        "default",
        "Default",
        "Default parity line-art pipeline: projection, face ownership visibility, edge fragments, path joining, draw progress, and comic ink strokes.",
        true,
        new Version(1, 0, 0),
        new Version(1, 0, 0),
        "STFU",
        ["default", "comic", "ink", "line-art"],
        PresetPackaging.BuiltInAot);

    public NprSettings CreateSettings()
    {
        var settings = new NprSettings
        {
            Seed = 17,
            CreaseAngleDegrees = 34f,
            MinimumProjectedTriangleArea = 0f,
            MinimumStrokeLength = 1f,
            HiddenLineDepthBias = 0.0007f,
            FeatureLineDensity = 1f,
            MainFillEnabled = false
        };

        settings.DefaultDrawing.ShowSilhouette = true;
        settings.DefaultDrawing.ShowFeature = true;
        settings.DefaultDrawing.ShowBoundary = true;
        settings.DefaultDrawing.TopologyMode = DefaultTopologyMode.PerTriangleEdges;
        settings.DefaultDrawing.FeatureAngleDegrees = 34f;
        settings.DefaultDrawing.CullOutside = true;
        settings.DefaultDrawing.MinSegPx = 1f;
        settings.DefaultDrawing.MeshStride = 1;
        settings.DefaultDrawing.OcclusionCulling = true;
        settings.DefaultDrawing.OcclusionSamples = 7;
        settings.DefaultDrawing.OcclusionStrictness = 1f;
        settings.DefaultDrawing.OcclusionBias = 0.0007f;
        settings.DefaultDrawing.DepthScale = 1f;
        settings.DefaultDrawing.StrokeStyle = DefaultStrokeStyle.ComicInk;
        settings.DefaultDrawing.StrokeColor = new StrokeColor(0x23, 0x20, 0x1c);
        settings.DefaultDrawing.PaperColor = new StrokeColor(0xe8, 0xe2, 0xd5);
        settings.DefaultDrawing.LineWidth = 2.2f;
        settings.DefaultDrawing.Jitter = 1.6f;
        settings.DefaultDrawing.Pressure = 0.32f;
        settings.DefaultDrawing.PathSimplify = 0.6f;
        settings.DefaultDrawing.AutoDraw = true;
        settings.DefaultDrawing.DrawSpeed = 0.28f;
        settings.DefaultDrawing.DrawProgress = 1f;
        settings.DefaultDrawing.EnableFastNoise = true;
        settings.DefaultDrawing.FieldOfViewDegrees = 45f;
        settings.DefaultDrawing.NearPlane = 0.01f;
        settings.DefaultDrawing.FarPlane = 1000f;

        return settings;
    }

    public StyleGrammar CreateGrammar()
    {
        return new StyleGrammar(
            "default",
            "Default",
            new Version(1, 0, 0),
            [
                new StyleFeatureRule(FeatureCurveKind.Silhouette, true, 1f, 0f, HiddenLinePolicy.Suppress, NprStrokeIntent.Silhouette, 10, "silhouette"),
                new StyleFeatureRule(FeatureCurveKind.Crease, true, 1f, 0f, HiddenLinePolicy.Suppress, NprStrokeIntent.Crease, 20, "feature"),
                new StyleFeatureRule(FeatureCurveKind.Boundary, true, 1f, 0f, HiddenLinePolicy.Suppress, NprStrokeIntent.Boundary, 30, "boundary")
            ],
            new StyleVisibilityRule(
                VisibilityStrictness.Sampled,
                0.0007f,
                SplitCurves: true,
                KeepHiddenSegmentsForDebug: true,
                DefaultHiddenPolicy: HiddenLinePolicy.Suppress),
            new StyleToneRule(false, 0f, 0f, 1f, 1f),
            new StyleHatchingRule(false, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, false),
            new StyleStrokeRule(
                [
                    new StyleStrokeProfile(NprStrokeIntent.Silhouette, 2.2f * 1.68f, 0.98f, new StrokeColor(0x23, 0x20, 0x1c)),
                    new StyleStrokeProfile(NprStrokeIntent.Crease, 2.2f * 0.74f, 0.98f, new StrokeColor(0x23, 0x20, 0x1c)),
                    new StyleStrokeProfile(NprStrokeIntent.Boundary, 2.2f * 1.02f, 0.98f, new StrokeColor(0x23, 0x20, 0x1c))
                ],
                1f,
                1f),
            new StyleBudgetRule(96, int.MaxValue, true),
            new StyleExportRule(
                SvgExportMode.Editable,
                IncludeMetadata: true,
                IncludeDebugLayers: false,
                Units: "px",
                PreferredLayers: ["silhouette", "feature", "boundary"]),
            new StyleDebugRule([
                STFU.NPR.Debug.DebugOverlayKind.FeatureCurves,
                STFU.NPR.Debug.DebugOverlayKind.VisibilitySegments,
                STFU.NPR.Debug.DebugOverlayKind.StrokeCandidates
            ]));
    }

    public NprStyleSet CreateStyleSet()
    {
        var paper = new NprPaper(new StrokeColor(0xe8, 0xe2, 0xd5), 1f);
        var foreground = new NprRoleStyle(
            NprSceneRole.Foreground,
            1f,
            1f,
            1f,
            1f,
            1f,
            [
                StrokeLayer("silhouette", "Silhouette", 10, 1f),
                StrokeLayer("feature", "Feature", 20, 1f),
                StrokeLayer("boundary", "Boundary", 30, 1f)
            ]);

        return new NprStyleSet(Metadata.Id, Metadata.Name, paper, foreground, foreground, foreground);
    }

    private static NprLayerStyle StrokeLayer(string id, string name, int order, float opacity)
    {
        return new NprLayerStyle(
            id,
            name,
            order,
            true,
            opacity,
            NprLayerBlendMode.Normal,
            new NprToneStyle(false, new StrokeColor(0, 0, 0), 0f, 0f),
            new NprShadingStyle(false, 0f, 0f),
            new NprStrokeChannelStyle(true, 1f, 1f),
            new NprStrokeChannelStyle(true, 1f, 1f),
            new NprStrokeChannelStyle(true, 1f, 1f));
    }
}