using STFU.NPR.Pipeline;
using STFU.NPR.Settings;
using STFU.NPR.Debug;
using STFU.NPR.Graph;
using STFU.NPR.Steps.Analysis;
using STFU.NPR.Steps.Debug;
using STFU.NPR.Steps.Mesh;
using STFU.NPR.Steps.Strokes;
using STFU.Strokes;
using STFU.Strokes.Export;

namespace STFU.NPR.Composition;

public static class SketchNprPreset
{
    public static NprSettings CreateSettings()
    {
        var settings = new NprSettings
        {
            Seed = 1337,
            CreaseAngleDegrees = 34f,
            MinimumProjectedTriangleArea = 8f,
            MinimumStrokeLength = 4f,
            SurfaceFlowShadeThreshold = 0.52f,
            SurfaceFlowDensity = 0.38f,
            HatchShadeThreshold = 0.58f,
            HatchDensity = 0.48f,
            HatchLength = 19f,
            HiddenLineDepthBias = 0.025f,
            NearClipDepth = 0.05f,
            FarClipDepth = 500f,
            ScreenClipMarginPixels = 128f,
            MaxProjectedTriangleAreaRatio = 6f,
            FeatureLineDensity = 0.86f
        };

        settings.StrokeStyle.BaseThickness = 1.28f;
        settings.StrokeStyle.ThicknessVariation = 0.42f;
        settings.StrokeStyle.EndpointJitter = 0.95f;
        settings.StrokeStyle.Overshoot = 1.85f;

        return settings;
    }

    public static StyleGrammar CreateGrammar()
    {
        return new StyleGrammar(
            "generic-sketch",
            "Generic Sketch",
            new Version(1, 0, 0),
            [
                new StyleFeatureRule(FeatureCurveKind.Boundary, true, 1.0f, 0.0f, HiddenLinePolicy.Suppress, NprStrokeIntent.Boundary, 10, "boundary"),
                new StyleFeatureRule(FeatureCurveKind.Silhouette, true, 1.0f, 0.0f, HiddenLinePolicy.Suppress, NprStrokeIntent.Silhouette, 20, "silhouette"),
                new StyleFeatureRule(FeatureCurveKind.OccludingContour, true, 1.0f, 0.0f, HiddenLinePolicy.Suppress, NprStrokeIntent.Silhouette, 21, "occluding-contour"),
                new StyleFeatureRule(FeatureCurveKind.Crease, true, 0.8f, 0.34f, HiddenLinePolicy.Suppress, NprStrokeIntent.Crease, 30, "crease"),
                new StyleFeatureRule(FeatureCurveKind.MaterialBoundary, true, 0.74f, 0.33f, HiddenLinePolicy.Suppress, NprStrokeIntent.Accent, 32, "material-boundary"),
                new StyleFeatureRule(FeatureCurveKind.ContactAccent, true, 0.64f, 0.26f, HiddenLinePolicy.Suppress, NprStrokeIntent.Accent, 33, "contact-accent"),
                new StyleFeatureRule(FeatureCurveKind.ApparentRidge, true, 0.82f, 0.36f, HiddenLinePolicy.Suppress, NprStrokeIntent.Accent, 34, "apparent-ridge"),
                new StyleFeatureRule(FeatureCurveKind.Ridge, true, 0.72f, 0.40f, HiddenLinePolicy.Suppress, NprStrokeIntent.Accent, 35, "ridge"),
                new StyleFeatureRule(FeatureCurveKind.Valley, true, 0.68f, 0.42f, HiddenLinePolicy.Suppress, NprStrokeIntent.Accent, 36, "valley"),
                new StyleFeatureRule(FeatureCurveKind.SuggestiveContour, true, 0.78f, 0.38f, HiddenLinePolicy.Suppress, NprStrokeIntent.Accent, 37, "suggestive-contour"),
                new StyleFeatureRule(FeatureCurveKind.Construction, true, 0.24f, 0.12f, HiddenLinePolicy.Ghost, NprStrokeIntent.Accent, 38, "construction"),
                new StyleFeatureRule(FeatureCurveKind.HatchGuide, true, 0.22f, 0.14f, HiddenLinePolicy.Suppress, NprStrokeIntent.Hatch, 39, "hatch-guide"),
                new StyleFeatureRule(FeatureCurveKind.Hatch, true, 0.55f, 0.30f, HiddenLinePolicy.Suppress, NprStrokeIntent.Hatch, 40, "hatch"),
                new StyleFeatureRule(FeatureCurveKind.SurfaceFlow, true, 0.5f, 0.28f, HiddenLinePolicy.Suppress, NprStrokeIntent.SurfaceFlow, 50, "surface-flow")
            ],
            new StyleVisibilityRule(
                VisibilityStrictness.Sampled,
                0.025f,
                SplitCurves: false,
                KeepHiddenSegmentsForDebug: true,
                DefaultHiddenPolicy: HiddenLinePolicy.KeepForDebug),
            new StyleToneRule(
                Enabled: true,
                ToneInfluence: 0.25f,
                ShadeInfluence: 0.35f,
                MinimumOpacity: 0.06f,
                MaximumOpacity: 1f),
            new StyleHatchingRule(
                Enabled: true,
                ToneThreshold: 0.58f,
                CrossHatchThreshold: 0.74f,
                DeepShadowThreshold: 0.88f,
                DensityScale: 1f,
                BaseSpacingPixels: 14f,
                StrokeLengthPixels: 19f,
                DirectionAngleOffsetRadians: -0.78f,
                CrossAngleOffsetRadians: 1.18f,
                TertiaryAngleOffsetRadians: 0.26f,
                JitterRadians: 0.32f,
                UseDirectionField: true),
            new StyleStrokeRule(
                [
                    new StyleStrokeProfile(NprStrokeIntent.Silhouette, 2.1f, 0.95f, new StrokeColor(12, 12, 12)),
                    new StyleStrokeProfile(NprStrokeIntent.Boundary, 1.8f, 0.85f, new StrokeColor(18, 18, 18)),
                    new StyleStrokeProfile(NprStrokeIntent.Crease, 1.55f, 0.72f, new StrokeColor(28, 28, 26)),
                    new StyleStrokeProfile(NprStrokeIntent.SurfaceFlow, 0.75f, 0.34f, new StrokeColor(62, 62, 58)),
                    new StyleStrokeProfile(NprStrokeIntent.Hatch, 0.65f, 0.35f, new StrokeColor(48, 48, 45)),
                    new StyleStrokeProfile(NprStrokeIntent.Accent, 1.15f, 0.58f, new StrokeColor(34, 31, 29))
                ],
                ThicknessScale: 1f,
                OpacityScale: 1f),
            new StyleBudgetRule(
                TileSizePixels: 96,
                MaxSegmentsPerTile: 14,
                AlwaysKeepPrimaryContours: true),
            new StyleExportRule(
                SvgExportMode.Editable,
                IncludeMetadata: true,
                IncludeDebugLayers: false,
                Units: "px",
                PreferredLayers: ["silhouette", "boundary", "crease", "surface-flow", "hatch"]),
            new StyleDebugRule([
                DebugOverlayKind.FeatureCurves,
                DebugOverlayKind.VisibilitySegments,
                DebugOverlayKind.SalienceHeatmap,
                DebugOverlayKind.StrokeCandidates,
                DebugOverlayKind.ToneField,
                DebugOverlayKind.DirectionField,
                DebugOverlayKind.DensityField,
                DebugOverlayKind.TextureField,
                DebugOverlayKind.TemporalMatches,
                DebugOverlayKind.GhostStrokes,
                DebugOverlayKind.HatchingPlan,
                DebugOverlayKind.StyleMask,
                DebugOverlayKind.MaterialRegion]));
    }

    public static INprPipeline CreatePipeline()
    {
        return new NprPipeline(
        [
            new ProjectMeshStep(),
            new BuildProjectedTrianglesStep(),
            new BuildMeshTopologyStep(),
            new BuildMaterialRegionsStep(),
            new ExtractFeatureCurvesStep(),
            new RefineFeatureConfidenceStep(),
            new BuildSurfaceSamplesStep(),
            new BuildScreenSpaceFieldsStep(),
            new BuildContactAccentsStep(),
            new BuildStyleMasksStep(),
            new BuildSurfaceFlowLinesStep(),
            new BuildHatchingStep(),
            new ApplyApproximateOcclusionStep(),
            new ScoreFeatureSalienceStep(),
            new PruneFeatureLinesStep(),
            new BuildStrokeCandidatesStep(),
            new BuildTemporalMatchesStep(),
            new StyleStrokesStep(),
            new HumanizeStrokesStep(),
            new BuildStrokeFrameStep(),
            new CaptureFrameHistoryStep(),
            new BuildDebugFrameStep()
        ]);
    }
}
