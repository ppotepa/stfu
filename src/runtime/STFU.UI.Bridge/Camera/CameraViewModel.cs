using System.Numerics;
using System.Windows.Input;
using STFU.Camera.Commands;
using STFU.Common.Math;
using STFU.UI.Bridge.Binding;
using STFU.UI.Bridge.Session;

namespace STFU.UI.Bridge.Camera;

public sealed class CameraViewModel : BindableObject
{
    private readonly STFU.Camera.CameraRig _camera;
    private readonly UiCommandBus _commands;
    private bool _isRefreshing;
    private float _positionX;
    private float _positionY;
    private float _positionZ;
    private float _targetX;
    private float _targetY;
    private float _targetZ;
    private float _fieldOfViewDegrees;
    private float _orbitYawDegrees;
    private float _orbitPitchDegrees;

    public CameraViewModel(STFU.Camera.CameraRig camera, UiCommandBus commands)
    {
        _camera = camera;
        _commands = commands;
        ResetCommand = new RelayCommand(Reset);
        FrameModelCommand = new RelayCommand(FrameModel);
        OrbitStepCommand = new RelayCommand(() => Orbit(NumericMath.DegreesToRadians(12f), 0f));
        PanStepCommand = new RelayCommand(() => Pan(0.15f, 0.05f));
        RefreshFromEngine();
    }

    public ICommand ResetCommand { get; }

    public ICommand FrameModelCommand { get; }

    public ICommand OrbitStepCommand { get; }

    public ICommand PanStepCommand { get; }

    public float PositionX
    {
        get => _positionX;
        set => SetCameraProperty(ref _positionX, value);
    }

    public float PositionY
    {
        get => _positionY;
        set => SetCameraProperty(ref _positionY, value);
    }

    public float PositionZ
    {
        get => _positionZ;
        set => SetCameraProperty(ref _positionZ, value);
    }

    public float TargetX
    {
        get => _targetX;
        set => SetCameraProperty(ref _targetX, value);
    }

    public float TargetY
    {
        get => _targetY;
        set => SetCameraProperty(ref _targetY, value);
    }

    public float TargetZ
    {
        get => _targetZ;
        set => SetCameraProperty(ref _targetZ, value);
    }

    public float FieldOfViewDegrees
    {
        get => _fieldOfViewDegrees;
        set => SetCameraProperty(ref _fieldOfViewDegrees, NumericMath.Clamp(value, 1f, 179f));
    }

    public float OrbitYawDegrees
    {
        get => _orbitYawDegrees;
        set
        {
            var delta = value - _orbitYawDegrees;
            if (NumericMath.Abs(delta) < 0.001f)
            {
                return;
            }

            _orbitYawDegrees = value;
            OnPropertyChanged();
            Orbit(NumericMath.DegreesToRadians(delta), 0f);
        }
    }

    public float OrbitPitchDegrees
    {
        get => _orbitPitchDegrees;
        set
        {
            var delta = value - _orbitPitchDegrees;
            if (NumericMath.Abs(delta) < 0.001f)
            {
                return;
            }

            _orbitPitchDegrees = value;
            OnPropertyChanged();
            Orbit(0f, NumericMath.DegreesToRadians(delta));
        }
    }

    public string CompactLabel =>
        $"Camera ({PositionX:0.##}, {PositionY:0.##}, {PositionZ:0.##}) -> ({TargetX:0.##}, {TargetY:0.##}, {TargetZ:0.##}), {FieldOfViewDegrees:0.#} deg";

    public void Orbit(float deltaYawRadians, float deltaPitchRadians)
    {
        _commands.Execute(
            new OrbitCameraCommand(deltaYawRadians, deltaPitchRadians),
            $"OrbitCameraCommand(yaw={deltaYawRadians:0.###}, pitch={deltaPitchRadians:0.###})");
        RefreshFromEngine();
    }

    public void Pan(float deltaRight, float deltaUp)
    {
        _commands.Execute(
            new PanCameraCommand(deltaRight, deltaUp),
            $"PanCameraCommand(right={deltaRight:0.###}, up={deltaUp:0.###})");
        RefreshFromEngine();
    }

    public void AdjustFieldOfView(float deltaDegrees)
    {
        _commands.Execute(
            new AdjustCameraFovCommand(deltaDegrees),
            $"AdjustCameraFovCommand(delta={deltaDegrees:0.###})");
        RefreshFromEngine();
    }

    public void Reset()
    {
        SetCamera(STFU.Camera.CameraState.Default, "SetCameraCommand(CameraState.Default)");
        ResetOrbitSliders();
    }

    public void FrameModel()
    {
        SetCamera(
            new STFU.Camera.CameraState(
                new Vector3(0f, 0.3f, 3.2f),
                new Vector3(0f, 0.1f, 0f),
                52f),
            "SetCameraCommand(Frame Model)");
        ResetOrbitSliders();
    }

    public void RefreshFromEngine()
    {
        _isRefreshing = true;
        var camera = _camera.Camera;
        SetProperty(ref _positionX, camera.Position.X, nameof(PositionX));
        SetProperty(ref _positionY, camera.Position.Y, nameof(PositionY));
        SetProperty(ref _positionZ, camera.Position.Z, nameof(PositionZ));
        SetProperty(ref _targetX, camera.Target.X, nameof(TargetX));
        SetProperty(ref _targetY, camera.Target.Y, nameof(TargetY));
        SetProperty(ref _targetZ, camera.Target.Z, nameof(TargetZ));
        SetProperty(ref _fieldOfViewDegrees, camera.FieldOfViewDegrees, nameof(FieldOfViewDegrees));
        OnPropertyChanged(nameof(CompactLabel));
        _isRefreshing = false;
    }

    private void SetCameraProperty(ref float storage, float value)
    {
        if (!SetProperty(ref storage, value))
        {
            return;
        }

        OnPropertyChanged(nameof(CompactLabel));
        if (!_isRefreshing)
        {
            CommitCamera();
        }
    }

    private void CommitCamera()
    {
        SetCamera(CreateCameraState(), "SetCameraCommand(CameraState from UI binding)");
    }

    private void SetCamera(STFU.Camera.CameraState camera, string label)
    {
        _commands.Execute(new SetCameraCommand(camera), label);
        RefreshFromEngine();
    }

    private STFU.Camera.CameraState CreateCameraState()
    {
        return new STFU.Camera.CameraState(
            new Vector3(PositionX, PositionY, PositionZ),
            new Vector3(TargetX, TargetY, TargetZ),
            FieldOfViewDegrees);
    }

    private void ResetOrbitSliders()
    {
        SetProperty(ref _orbitYawDegrees, 0f, nameof(OrbitYawDegrees));
        SetProperty(ref _orbitPitchDegrees, 0f, nameof(OrbitPitchDegrees));
    }

}
