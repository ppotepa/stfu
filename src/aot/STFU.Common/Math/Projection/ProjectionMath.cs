using System.Numerics;

namespace STFU.Common.Math;

public readonly record struct ProjectionScalars(
    float NearPlane,
    float FarPlane,
    float NdcScaleX,
    float NdcScaleY,
    float DepthRangeInv,
    float ScreenScaleX,
    float ScreenScaleY,
    float ScreenOffsetX,
    float ScreenOffsetY);

public readonly record struct ProjectedPoint(
    Vector2 Screen,
    Vector3 Ndc,
    float Depth,
    float Depth01,
    bool IsVisible);

public static class ProjectionMath
{
    public static ProjectionScalars CreatePerspectiveScalars(
        int width,
        int height,
        float fieldOfViewDegrees,
        float nearPlane,
        float farPlane)
    {
        var fovRadians = NumericMath.DegreesToRadians(global::System.Math.Clamp(fieldOfViewDegrees, 1f, 179f));
        var focalScale = 1f / MathF.Tan(fovRadians * 0.5f);
        var aspect = width / (float)global::System.Math.Max(1, height);
        var clippedNear = global::System.Math.Max(0.001f, nearPlane);
        var clippedFar = global::System.Math.Max(clippedNear + 0.001f, farPlane);
        var screenScaleX = width * 0.5f;
        var screenScaleY = height * 0.5f;

        return new ProjectionScalars(
            clippedNear,
            clippedFar,
            focalScale / aspect,
            focalScale,
            1f / global::System.Math.Max(0.0001f, clippedFar - clippedNear),
            screenScaleX,
            -screenScaleY,
            screenScaleX,
            screenScaleY);
    }

    public static ProjectedPoint Project(
        Vector3 worldPosition,
        Vector3 cameraPosition,
        CameraBasis basis,
        ProjectionScalars scalars)
    {
        var cameraSpace = worldPosition - cameraPosition;
        var depth = Vector3.Dot(cameraSpace, basis.Forward);

        if (depth <= scalars.NearPlane || depth >= scalars.FarPlane || !float.IsFinite(depth))
        {
            return new ProjectedPoint(default, default, depth, 1f, false);
        }

        var x = Vector3.Dot(cameraSpace, basis.Right);
        var y = Vector3.Dot(cameraSpace, basis.Up);
        var invDepth = 1f / depth;
        var normalizedX = x * invDepth * scalars.NdcScaleX;
        var normalizedY = y * invDepth * scalars.NdcScaleY;
        var depth01 = global::System.Math.Clamp((depth - scalars.NearPlane) * scalars.DepthRangeInv, 0f, 1f);
        var screen = new Vector2(
            normalizedX * scalars.ScreenScaleX + scalars.ScreenOffsetX,
            normalizedY * scalars.ScreenScaleY + scalars.ScreenOffsetY);
        var visible = float.IsFinite(screen.X) && float.IsFinite(screen.Y);

        return new ProjectedPoint(
            screen,
            new Vector3(normalizedX, normalizedY, depth01 * 2f - 1f),
            depth,
            depth01,
            visible);
    }
}
