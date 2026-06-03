using System.Numerics;

namespace STFU.Camera;

public sealed class CameraRig
{
    private const float MinPitchRadians = -1.45f;
    private const float MaxPitchRadians = 1.45f;
    private const float MinFovDegrees = 20f;
    private const float MaxFovDegrees = 100f;

    private Vector3 _target = Vector3.Zero;
    private float _yawRadians;
    private float _pitchRadians;
    private float _distance = 4f;
    private float _fieldOfViewDegrees = 60f;

    public CameraState Camera { get; private set; } = CameraState.Default;

    public void SetCamera(CameraState camera)
    {
        _target = camera.Target;
        _fieldOfViewDegrees = Clamp(camera.FieldOfViewDegrees, MinFovDegrees, MaxFovDegrees);

        var offset = camera.Position - camera.Target;
        _distance = MathF.Max(0.001f, offset.Length());

        if (_distance > 0.001f)
        {
            var direction = Vector3.Normalize(offset);
            _pitchRadians = MathF.Asin(Clamp(direction.Y, -1f, 1f));
            _yawRadians = MathF.Atan2(direction.X, direction.Z);
        }

        Camera = camera;
    }

    public void Orbit(float deltaYawRadians, float deltaPitchRadians)
    {
        _yawRadians += deltaYawRadians;
        _pitchRadians = Clamp(_pitchRadians + deltaPitchRadians, MinPitchRadians, MaxPitchRadians);
        UpdateCamera();
    }

    public void Pan(float deltaRight, float deltaUp)
    {
        var forward = Vector3.Normalize(Camera.Target - Camera.Position);
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
        var offset = right * deltaRight + up * deltaUp;

        _target += offset;
        Camera = new CameraState(Camera.Position + offset, Camera.Target + offset, _fieldOfViewDegrees);
    }

    public void AdjustFieldOfView(float deltaDegrees)
    {
        _fieldOfViewDegrees = Clamp(_fieldOfViewDegrees + deltaDegrees, MinFovDegrees, MaxFovDegrees);
        UpdateCamera();
    }

    private void UpdateCamera()
    {
        var cosPitch = MathF.Cos(_pitchRadians);
        var offset = new Vector3(
            MathF.Sin(_yawRadians) * cosPitch,
            MathF.Sin(_pitchRadians),
            MathF.Cos(_yawRadians) * cosPitch) * _distance;

        Camera = new CameraState(_target + offset, _target, _fieldOfViewDegrees);
    }

    private static float Clamp(float value, float min, float max) => MathF.Min(max, MathF.Max(min, value));
}
