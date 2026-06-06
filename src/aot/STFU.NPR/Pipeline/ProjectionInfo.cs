using System.Numerics;
using STFU.Camera;
using STFU.Common.Math;
using STFU.Strokes;

namespace STFU.NPR.Pipeline;

public readonly record struct ProjectionInfo(
    Vector3 Position,
    Vector3 Forward,
    Vector3 Right,
    Vector3 Up,
    int Width,
    int Height,
    float NearPlane,
    float FarPlane,
    float NdcScaleX,
    float NdcScaleY,
    float DepthRangeInv,
    float ScreenScaleX,
    float ScreenScaleY,
    float ScreenOffsetX,
    float ScreenOffsetY)
{
    public static ProjectionInfo Create(CameraState camera, int width, int height, Settings.NprSettings? settings = null)
    {
        var drawing = settings?.DefaultDrawing;
        var basis = CameraMath.CreateBasis(camera.Position, camera.Target);
        var scalars = ProjectionMath.CreatePerspectiveScalars(
            width,
            height,
            camera.FieldOfViewDegrees,
            drawing?.NearPlane ?? settings?.NearClipDepth ?? 0.05f,
            drawing?.FarPlane ?? settings?.FarClipDepth ?? 500f);

        return new ProjectionInfo(
            camera.Position,
            basis.Forward,
            basis.Right,
            basis.Up,
            width,
            height,
            scalars.NearPlane,
            scalars.FarPlane,
            scalars.NdcScaleX,
            scalars.NdcScaleY,
            scalars.DepthRangeInv,
            scalars.ScreenScaleX,
            scalars.ScreenScaleY,
            scalars.ScreenOffsetX,
            scalars.ScreenOffsetY);
    }

    public bool TryProject(Vector3 worldPosition, out Point2D point, out float depth)
    {
        var ok = TryProject(worldPosition, out point, out depth, out _, out _);
        return ok;
    }

    public bool TryProject(
        Vector3 worldPosition,
        out Point2D point,
        out float depth,
        out Vector3 ndc,
        out float depth01)
    {
        var projected = ProjectionMath.Project(
            worldPosition,
            Position,
            new CameraBasis(Forward, Right, Up),
            new ProjectionScalars(
                NearPlane,
                FarPlane,
                NdcScaleX,
                NdcScaleY,
                DepthRangeInv,
                ScreenScaleX,
                ScreenScaleY,
                ScreenOffsetX,
                ScreenOffsetY));
        depth = projected.Depth;
        depth01 = projected.Depth01;
        ndc = projected.Ndc;

        if (!projected.IsVisible)
        {
            point = default;
            return false;
        }

        point = new Point2D(projected.Screen.X, projected.Screen.Y);
        return true;
    }
}
