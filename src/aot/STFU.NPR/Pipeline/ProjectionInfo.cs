using System.Numerics;
using STFU.Camera;
using STFU.Strokes;

namespace STFU.NPR.Pipeline;

public readonly record struct ProjectionInfo(
    Vector3 Position,
    Vector3 Forward,
    Vector3 Right,
    Vector3 Up,
    float FocalScale,
    float Aspect,
    int Width,
    int Height,
    float NearClipDepth,
    float FarClipDepth)
{
    public static ProjectionInfo Create(CameraState camera, int width, int height, Settings.NprSettings? settings = null)
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
        var nearClip = Math.Max(0.001f, settings?.NearClipDepth ?? 0.05f);
        var farClip = Math.Max(nearClip + 0.001f, settings?.FarClipDepth ?? 500f);

        return new ProjectionInfo(camera.Position, forward, right, up, focalScale, aspect, width, height, nearClip, farClip);
    }

    public bool TryProject(Vector3 worldPosition, out Point2D point, out float depth)
    {
        var cameraSpace = worldPosition - Position;
        depth = Vector3.Dot(cameraSpace, Forward);

        if (depth < NearClipDepth || depth > FarClipDepth || !float.IsFinite(depth))
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

        return float.IsFinite(point.X) && float.IsFinite(point.Y);
    }
}
