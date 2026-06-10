using STFU.NPR.Pipeline;

namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public static class InteractiveFrameSignatureFactory
{
    public static InteractiveFrameSignature FromContext(
        NprContext context,
        int width,
        int height)
    {
        ArgumentNullException.ThrowIfNull(context);

        return new InteractiveFrameSignature(
            ContentHash: ComputeContentHash(context),
            CameraHash: ComputeCameraHash(context),
            StyleHash: ComputeStyleHash(context),
            ViewportHash: ComputeViewportHash(width, height),
            DebugHash: ComputeDebugHash(context));
    }

    private static ulong ComputeContentHash(NprContext context)
    {
        var hash = InteractiveFrameHasher.Empty;
        hash = InteractiveFrameHasher.Mix(hash, context.Scene.Entities.Count);

        hash = InteractiveFrameHasher.Mix(hash, (int)context.EntityStyles.DefaultRole);

        foreach (var entity in context.Scene.Entities)
        {
            hash = InteractiveFrameHasher.MixEntity(hash, entity);
            hash = InteractiveFrameHasher.Mix(hash, (int)context.EntityStyles.GetRole(entity.Id));
        }

        return hash;
    }

    private static ulong ComputeCameraHash(NprContext context)
    {
        var camera = context.Camera;
        var hash = InteractiveFrameHasher.Empty;
        hash = InteractiveFrameHasher.Mix(hash, camera.Position);
        hash = InteractiveFrameHasher.Mix(hash, camera.Target);
        hash = InteractiveFrameHasher.Mix(hash, camera.FieldOfViewDegrees);
        return hash;
    }

    private static ulong ComputeStyleHash(NprContext context)
    {
        var drawing = context.Settings.DefaultDrawing;
        var hash = InteractiveFrameHasher.Empty;

        hash = InteractiveFrameHasher.Mix(hash, context.Style.StyleId);
        hash = InteractiveFrameHasher.Mix(hash, context.StyleSet.Id);
        hash = InteractiveFrameHasher.Mix(hash, context.Settings.Seed);
        hash = InteractiveFrameHasher.Mix(hash, context.Settings.CreaseAngleDegrees);
        hash = InteractiveFrameHasher.Mix(hash, context.Settings.MinimumStrokeLength);
        hash = InteractiveFrameHasher.Mix(hash, context.Settings.MainFillEnabled);

        hash = InteractiveFrameHasher.Mix(hash, (int)drawing.TopologyMode);
        hash = InteractiveFrameHasher.Mix(hash, drawing.ShowSilhouette);
        hash = InteractiveFrameHasher.Mix(hash, drawing.ShowFeature);
        hash = InteractiveFrameHasher.Mix(hash, drawing.ShowBoundary);
        hash = InteractiveFrameHasher.Mix(hash, drawing.FeatureAngleDegrees);
        hash = InteractiveFrameHasher.Mix(hash, drawing.MinSegPx);
        hash = InteractiveFrameHasher.Mix(hash, drawing.MeshStride);
        hash = InteractiveFrameHasher.Mix(hash, drawing.OcclusionCulling);
        hash = InteractiveFrameHasher.Mix(hash, drawing.OcclusionSamples);
        hash = InteractiveFrameHasher.Mix(hash, drawing.OcclusionStrictness);
        hash = InteractiveFrameHasher.Mix(hash, drawing.OcclusionBias);
        hash = InteractiveFrameHasher.Mix(hash, drawing.DepthScale);
        hash = InteractiveFrameHasher.Mix(hash, (int)drawing.StrokeStyle);
        hash = InteractiveFrameHasher.Mix(hash, drawing.LineWidth);
        hash = InteractiveFrameHasher.Mix(hash, drawing.Jitter);
        hash = InteractiveFrameHasher.Mix(hash, drawing.Pressure);
        hash = InteractiveFrameHasher.Mix(hash, drawing.PathSimplify);
        hash = InteractiveFrameHasher.Mix(hash, drawing.DrawProgress);
        hash = InteractiveFrameHasher.Mix(hash, drawing.DrawSpeed);

        return hash;
    }

    private static ulong ComputeViewportHash(int width, int height)
    {
        var hash = InteractiveFrameHasher.Empty;
        hash = InteractiveFrameHasher.Mix(hash, Math.Max(1, width));
        hash = InteractiveFrameHasher.Mix(hash, Math.Max(1, height));
        return hash;
    }

    private static ulong ComputeDebugHash(NprContext context)
    {
        var hash = InteractiveFrameHasher.Empty;
        hash = InteractiveFrameHasher.Mix(hash, context.IncludeDebugFrame);
        hash = InteractiveFrameHasher.Mix(hash, context.EnableDetailedStepNotes);
        hash = InteractiveFrameHasher.Mix(hash, context.EnableRangeTimings);
        hash = InteractiveFrameHasher.Mix(hash, context.EnableStepAllocationTracking);
        return hash;
    }
}
