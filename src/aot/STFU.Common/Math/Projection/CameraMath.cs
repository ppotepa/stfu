using System.Numerics;

namespace STFU.Common.Math;

public readonly record struct CameraBasis(
    Vector3 Forward,
    Vector3 Right,
    Vector3 Up);

public readonly record struct CameraOrbitState(
    Vector3 Target,
    float YawRadians,
    float PitchRadians,
    float Distance,
    float FieldOfViewDegrees);

public static class CameraMath
{
    public static CameraOrbitState CreateOrbitState(
        Vector3 position,
        Vector3 target,
        float fieldOfViewDegrees,
        float minFovDegrees,
        float maxFovDegrees,
        float minDistance = 0.001f)
    {
        var offset = position - target;
        var distance = MathF.Max(minDistance, offset.Length());
        var yaw = 0f;
        var pitch = 0f;

        if (distance > minDistance)
        {
            var direction = Vector3.Normalize(offset);
            pitch = MathF.Asin(NumericMath.Clamp(direction.Y, -1f, 1f));
            yaw = MathF.Atan2(direction.X, direction.Z);
        }

        return new CameraOrbitState(
            target,
            yaw,
            pitch,
            distance,
            NumericMath.Clamp(fieldOfViewDegrees, minFovDegrees, maxFovDegrees));
    }

    public static CameraBasis CreateBasis(Vector3 position, Vector3 target)
    {
        var forward = Geometry3D.NormalizeOrDefault(target - position, -Vector3.UnitZ);
        var right = Vector3.Cross(forward, Vector3.UnitY);
        right = Geometry3D.NormalizeOrDefault(right, Vector3.UnitX);
        var up = Geometry3D.NormalizeOrDefault(Vector3.Cross(right, forward), Vector3.UnitY);
        return new CameraBasis(forward, right, up);
    }

    public static Vector3 CreateOrbitOffset(float yawRadians, float pitchRadians, float distance)
    {
        var cosPitch = MathF.Cos(pitchRadians);
        return new Vector3(
            MathF.Sin(yawRadians) * cosPitch,
            MathF.Sin(pitchRadians),
            MathF.Cos(yawRadians) * cosPitch) * distance;
    }

    public static Vector3 CreatePanOffset(Vector3 position, Vector3 target, float deltaRight, float deltaUp)
    {
        var basis = CreateBasis(position, target);
        return basis.Right * deltaRight + basis.Up * deltaUp;
    }
}
