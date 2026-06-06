using STFU.Common.Math;
using STFU.NPR.Composition;
using STFU.UI.Bridge.Binding;
using STFU.UI.Bridge.Session;
using System.Windows.Input;

namespace STFU.UI.Bridge.Npr;

public sealed class DefaultDrawingViewModel : BindableObject
{
    private readonly ActiveNprPresetState _activePreset;
    private readonly UiCommandBus _commands;

    public DefaultDrawingViewModel(ActiveNprPresetState activePreset, UiCommandBus commands)
    {
        _activePreset = activePreset;
        _commands = commands;
        ResetDrawProgressCommand = new RelayCommand(() => DrawProgress = 0f);
        FinishDrawProgressCommand = new RelayCommand(() => DrawProgress = 1f);
        ToggleAutoDrawCommand = new RelayCommand(() => AutoDraw = !AutoDraw);
    }

    public ICommand ResetDrawProgressCommand { get; }

    public ICommand FinishDrawProgressCommand { get; }

    public ICommand ToggleAutoDrawCommand { get; }

    public int Seed
    {
        get => _activePreset.ActiveSettings.Seed;
        set
        {
            if (_activePreset.ActiveSettings.Seed == value)
            {
                return;
            }

            _activePreset.ActiveSettings.Seed = value;
            NotifySettingChanged(nameof(Seed));
        }
    }

    public bool ShowSilhouette
    {
        get => Drawing.ShowSilhouette;
        set => SetDrawingBool(nameof(ShowSilhouette), (current, next) => current.ShowSilhouette = next, Drawing.ShowSilhouette, value);
    }

    public bool ShowFeature
    {
        get => Drawing.ShowFeature;
        set => SetDrawingBool(nameof(ShowFeature), (current, next) => current.ShowFeature = next, Drawing.ShowFeature, value);
    }

    public bool ShowBoundary
    {
        get => Drawing.ShowBoundary;
        set => SetDrawingBool(nameof(ShowBoundary), (current, next) => current.ShowBoundary = next, Drawing.ShowBoundary, value);
    }

    public bool OcclusionCulling
    {
        get => Drawing.OcclusionCulling;
        set => SetDrawingBool(nameof(OcclusionCulling), (current, next) => current.OcclusionCulling = next, Drawing.OcclusionCulling, value);
    }

    public bool AutoDraw
    {
        get => Drawing.AutoDraw;
        set => SetDrawingBool(nameof(AutoDraw), (current, next) => current.AutoDraw = next, Drawing.AutoDraw, value);
    }

    public float FeatureAngleDegrees
    {
        get => Drawing.FeatureAngleDegrees;
        set => SetDrawingFloat(nameof(FeatureAngleDegrees), (current, next) => current.FeatureAngleDegrees = next, Drawing.FeatureAngleDegrees, value);
    }

    public float MinSegPx
    {
        get => Drawing.MinSegPx;
        set => SetDrawingFloat(nameof(MinSegPx), (current, next) => current.MinSegPx = next, Drawing.MinSegPx, NumericMath.AtLeast(value, 0f));
    }

    public int MeshStride
    {
        get => Drawing.MeshStride;
        set
        {
            var next = NumericMath.AtLeast(value, 1);
            if (Drawing.MeshStride == next)
            {
                return;
            }

            Drawing.MeshStride = next;
            NotifySettingChanged(nameof(MeshStride));
        }
    }

    public float DepthScale
    {
        get => Drawing.DepthScale;
        set => SetDrawingFloat(nameof(DepthScale), (current, next) => current.DepthScale = next, Drawing.DepthScale, NumericMath.AtLeast(value, 0.05f));
    }

    public float PathSimplify
    {
        get => Drawing.PathSimplify;
        set => SetDrawingFloat(nameof(PathSimplify), (current, next) => current.PathSimplify = next, Drawing.PathSimplify, NumericMath.AtLeast(value, 0f));
    }

    public float DrawProgress
    {
        get => Drawing.DrawProgress;
        set => SetDrawingFloat(nameof(DrawProgress), (current, next) => current.DrawProgress = next, Drawing.DrawProgress, NumericMath.Clamp01(value));
    }

    public float LineWidth
    {
        get => Drawing.LineWidth;
        set => SetDrawingFloat(nameof(LineWidth), (current, next) => current.LineWidth = next, Drawing.LineWidth, NumericMath.AtLeast(value, 0.1f));
    }

    public float Jitter
    {
        get => Drawing.Jitter;
        set => SetDrawingFloat(nameof(Jitter), (current, next) => current.Jitter = next, Drawing.Jitter, NumericMath.AtLeast(value, 0f));
    }

    public float Pressure
    {
        get => Drawing.Pressure;
        set => SetDrawingFloat(nameof(Pressure), (current, next) => current.Pressure = next, Drawing.Pressure, NumericMath.Clamp01(value));
    }

    public void RefreshFromEngine()
    {
        OnPropertyChanged(nameof(Seed));
        OnPropertyChanged(nameof(ShowSilhouette));
        OnPropertyChanged(nameof(ShowFeature));
        OnPropertyChanged(nameof(ShowBoundary));
        OnPropertyChanged(nameof(OcclusionCulling));
        OnPropertyChanged(nameof(AutoDraw));
        OnPropertyChanged(nameof(FeatureAngleDegrees));
        OnPropertyChanged(nameof(MinSegPx));
        OnPropertyChanged(nameof(MeshStride));
        OnPropertyChanged(nameof(DepthScale));
        OnPropertyChanged(nameof(PathSimplify));
        OnPropertyChanged(nameof(DrawProgress));
        OnPropertyChanged(nameof(LineWidth));
        OnPropertyChanged(nameof(Jitter));
        OnPropertyChanged(nameof(Pressure));
    }

    private STFU.NPR.Settings.DefaultDrawingSettings Drawing => _activePreset.ActiveSettings.DefaultDrawing;

    private void SetDrawingBool(
        string propertyName,
        Action<STFU.NPR.Settings.DefaultDrawingSettings, bool> assign,
        bool oldValue,
        bool newValue)
    {
        if (oldValue == newValue)
        {
            return;
        }

        assign(Drawing, newValue);
        NotifySettingChanged(propertyName);
    }

    private void SetDrawingFloat(
        string propertyName,
        Action<STFU.NPR.Settings.DefaultDrawingSettings, float> assign,
        float oldValue,
        float newValue)
    {
        if (NumericMath.Abs(oldValue - newValue) < 0.0001f)
        {
            return;
        }

        assign(Drawing, newValue);
        NotifySettingChanged(propertyName);
    }

    private void NotifySettingChanged(string propertyName)
    {
        OnPropertyChanged(propertyName);
        _commands.Record($"DefaultDrawingSettings.{propertyName} updated");
    }
}
