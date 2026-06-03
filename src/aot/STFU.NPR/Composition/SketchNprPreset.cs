using STFU.NPR.Pipeline;
using STFU.NPR.Settings;
using STFU.NPR.Steps.Analysis;
using STFU.NPR.Steps.Mesh;
using STFU.NPR.Steps.Strokes;

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
            FeatureLineDensity = 0.86f
        };

        settings.StrokeStyle.BaseThickness = 1.28f;
        settings.StrokeStyle.ThicknessVariation = 0.42f;
        settings.StrokeStyle.EndpointJitter = 0.95f;
        settings.StrokeStyle.Overshoot = 1.85f;

        return settings;
    }

    public static INprPipeline CreatePipeline()
    {
        return new NprPipeline<
            ProjectMeshStep,
            BuildProjectedTrianglesStep,
            BuildMeshTopologyStep,
            ExtractFeatureLinesStep,
            BuildSurfaceSamplesStep,
            BuildSurfaceFlowLinesStep,
            BuildHatchingStep,
            ApplyApproximateOcclusionStep,
            PruneFeatureLinesStep,
            BuildStrokeCandidatesStep,
            StyleStrokesStep,
            HumanizeStrokesStep,
            BuildStrokeFrameStep>();
    }
}
