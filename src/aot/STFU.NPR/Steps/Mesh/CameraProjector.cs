using System.Numerics;
using STFU.Camera;
using STFU.Strokes;

namespace STFU.NPR.Steps.Mesh;

internal readonly record struct CameraProjector(
    Vector3 Position,
    Vector3 Forward,
    Vector3 Right,
    Vector3 Up,
    float FocalScale,
    float Aspect,
    int Width,
    int Height)
{
    public static CameraProjector Create(CameraState camera, int width, int height)
    {
        var forward = Vector3.Normalize(camera.Target - camera.Position);
        var right = Vector3.Cross(forward, Vector3.UnitY);

        if (right.LengthSquared() < 0.0001f)
        {
            right = Vector3.UnitX;
        }
        else
        {
            right = Vector3.Normalize(right);
        }

        var up = Vector3.Normalize(Vector3.Cross(right, forward));
        var fovRadians = MathF.PI / 180f * Math.Clamp(camera.FieldOfViewDegrees, 1f, 179f);
        var focalScale = 1f / MathF.Tan(fovRadians * 0.5f);
        var aspect = width / (float)Math.Max(1, height);

        return new CameraProjector(camera.Position, forward, right, up, focalScale, aspect, width, height);
    }

    public bool TryProject(Vector3 worldPosition, out Point2D point, out float depth)
    {
        var cameraSpace = worldPosition - Position;
        depth = Vector3.Dot(cameraSpace, Forward);

        if (depth <= 0.01f)
        {
            point = default;
            return false;
        }

        var x = Vector3.Dot(cameraSpace, Right);
        var y = Vector3.Dot(cameraSpace, Up);
        var normalizedX = x / depth * FocalScale / Aspect;
        var normalizedY = y / depth * FocalScale;

        point = new Point2D(
            (normalizedX * 0.5f + 0.5f) * Width,
            (-normalizedY * 0.5f + 0.5f) * Height);

        return true;
    }
}
