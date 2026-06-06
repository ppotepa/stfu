using System.Numerics;
using STFU.Common.Math;

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
        var orbit = CameraMath.CreateOrbitState(
            camera.Position,
            camera.Target,
            camera.FieldOfViewDegrees,
            MinFovDegrees,
            MaxFovDegrees);
        _target = orbit.Target;
        _fieldOfViewDegrees = orbit.FieldOfViewDegrees;
        _distance = orbit.Distance;
        _pitchRadians = orbit.PitchRadians;
        _yawRadians = orbit.YawRadians;

        Camera = camera;
    }

    public void Orbit(float deltaYawRadians, float deltaPitchRadians)
    {
        _yawRadians += deltaYawRadians;
        _pitchRadians = NumericMath.Clamp(_pitchRadians + deltaPitchRadians, MinPitchRadians, MaxPitchRadians);
        UpdateCamera();
    }

    public void Pan(float deltaRight, float deltaUp)
    {
        var offset = CameraMath.CreatePanOffset(Camera.Position, Camera.Target, deltaRight, deltaUp);

        _target += offset;
        Camera = new CameraState(Camera.Position + offset, Camera.Target + offset, _fieldOfViewDegrees);
    }

    public void AdjustFieldOfView(float deltaDegrees)
    {
        _fieldOfViewDegrees = NumericMath.Clamp(_fieldOfViewDegrees + deltaDegrees, MinFovDegrees, MaxFovDegrees);
        UpdateCamera();
    }

    private void UpdateCamera()
    {
        var offset = CameraMath.CreateOrbitOffset(_yawRadians, _pitchRadians, _distance);

        Camera = new CameraState(_target + offset, _target, _fieldOfViewDegrees);
    }
}
