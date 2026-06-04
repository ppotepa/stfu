using STFU.NPR.Settings;

namespace STFU.Rendering.Abstractions.Context;

public static class NprSettingsCloner
{
    public static NprSettings Clone(NprSettings source)
    {
        var copy = new NprSettings
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
            MinimumSalience = source.MinimumSalience,
            SurfaceBufferScale = source.SurfaceBufferScale,
            MainFillEnabled = source.MainFillEnabled
        };

        copy.StrokeStyle.Seed = source.StrokeStyle.Seed;
        copy.StrokeStyle.Medium = source.StrokeStyle.Medium;
        copy.StrokeStyle.BaseThickness = source.StrokeStyle.BaseThickness;
        copy.StrokeStyle.ThicknessVariation = source.StrokeStyle.ThicknessVariation;
        copy.StrokeStyle.EndpointJitter = source.StrokeStyle.EndpointJitter;
        copy.StrokeStyle.Overshoot = source.StrokeStyle.Overshoot;

        copy.DefaultDrawing.TopologyMode = source.DefaultDrawing.TopologyMode;
        copy.DefaultDrawing.ShowSilhouette = source.DefaultDrawing.ShowSilhouette;
        copy.DefaultDrawing.ShowFeature = source.DefaultDrawing.ShowFeature;
        copy.DefaultDrawing.ShowBoundary = source.DefaultDrawing.ShowBoundary;
        copy.DefaultDrawing.FeatureAngleDegrees = source.DefaultDrawing.FeatureAngleDegrees;
        copy.DefaultDrawing.CullOutside = source.DefaultDrawing.CullOutside;
        copy.DefaultDrawing.MinSegPx = source.DefaultDrawing.MinSegPx;
        copy.DefaultDrawing.MeshStride = source.DefaultDrawing.MeshStride;
        copy.DefaultDrawing.OcclusionCulling = source.DefaultDrawing.OcclusionCulling;
        copy.DefaultDrawing.OcclusionSamples = source.DefaultDrawing.OcclusionSamples;
        copy.DefaultDrawing.OcclusionStrictness = source.DefaultDrawing.OcclusionStrictness;
        copy.DefaultDrawing.OcclusionBias = source.DefaultDrawing.OcclusionBias;
        copy.DefaultDrawing.DepthScale = source.DefaultDrawing.DepthScale;
        copy.DefaultDrawing.StrokeStyle = source.DefaultDrawing.StrokeStyle;
        copy.DefaultDrawing.StrokeColor = source.DefaultDrawing.StrokeColor;
        copy.DefaultDrawing.PaperColor = source.DefaultDrawing.PaperColor;
        copy.DefaultDrawing.LineWidth = source.DefaultDrawing.LineWidth;
        copy.DefaultDrawing.Jitter = source.DefaultDrawing.Jitter;
        copy.DefaultDrawing.Pressure = source.DefaultDrawing.Pressure;
        copy.DefaultDrawing.PathSimplify = source.DefaultDrawing.PathSimplify;
        copy.DefaultDrawing.ShowPoints = source.DefaultDrawing.ShowPoints;
        copy.DefaultDrawing.AutoDraw = source.DefaultDrawing.AutoDraw;
        copy.DefaultDrawing.DrawSpeed = source.DefaultDrawing.DrawSpeed;
        copy.DefaultDrawing.DrawProgress = source.DefaultDrawing.DrawProgress;
        copy.DefaultDrawing.EnableFastNoise = source.DefaultDrawing.EnableFastNoise;
        copy.DefaultDrawing.FieldOfViewDegrees = source.DefaultDrawing.FieldOfViewDegrees;
        copy.DefaultDrawing.NearPlane = source.DefaultDrawing.NearPlane;
        copy.DefaultDrawing.FarPlane = source.DefaultDrawing.FarPlane;

        return copy;
    }
}
