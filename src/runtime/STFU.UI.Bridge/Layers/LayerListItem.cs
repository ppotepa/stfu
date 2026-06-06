using STFU.Common.Math;
using STFU.UI.Bridge.Binding;

namespace STFU.UI.Bridge.Layers;

public sealed class LayerListItem : BindableObject
{
    private string _name;
    private string _type;
    private string _blend;
    private bool _visible;
    private bool _solo;
    private bool _locked;
    private float _opacity;
    private float _density;
    private float _baseThickness;
    private float _thicknessVariation;
    private float _endpointJitter;
    private float _overshoot;
    private float _fillCoverage;
    private float _shadeThreshold;

    public LayerListItem(
        string id,
        string name,
        string role,
        string type,
        bool visible,
        float opacity,
        float density,
        string blend,
        bool locked = false,
        float baseThickness = 1.2f,
        float thicknessVariation = 0.25f,
        float endpointJitter = 0.5f,
        float overshoot = 0.4f,
        float fillCoverage = 0f,
        float shadeThreshold = 0.58f)
    {
        Id = id;
        _name = name;
        Role = role;
        _type = type;
        _visible = visible;
        _opacity = opacity;
        _density = density;
        _blend = blend;
        _locked = locked;
        _baseThickness = baseThickness;
        _thicknessVariation = thicknessVariation;
        _endpointJitter = endpointJitter;
        _overshoot = overshoot;
        _fillCoverage = fillCoverage;
        _shadeThreshold = shadeThreshold;
    }

    public string Id { get; }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Role { get; }

    public string Type
    {
        get => _type;
        set => SetProperty(ref _type, value);
    }

    public string Blend
    {
        get => _blend;
        set => SetProperty(ref _blend, value);
    }

    public bool Visible
    {
        get => _visible;
        set => SetProperty(ref _visible, value);
    }

    public bool Solo
    {
        get => _solo;
        set => SetProperty(ref _solo, value);
    }

    public bool Locked
    {
        get => _locked;
        set => SetProperty(ref _locked, value);
    }

    public float Opacity
    {
        get => _opacity;
        set => SetProperty(ref _opacity, NumericMath.Clamp01(value));
    }

    public float Density
    {
        get => _density;
        set => SetProperty(ref _density, NumericMath.AtLeast(value, 0f));
    }

    public float BaseThickness
    {
        get => _baseThickness;
        set => SetProperty(ref _baseThickness, NumericMath.AtLeast(value, 0.1f));
    }

    public float ThicknessVariation
    {
        get => _thicknessVariation;
        set => SetProperty(ref _thicknessVariation, NumericMath.Clamp01(value));
    }

    public float EndpointJitter
    {
        get => _endpointJitter;
        set => SetProperty(ref _endpointJitter, NumericMath.AtLeast(value, 0f));
    }

    public float Overshoot
    {
        get => _overshoot;
        set => SetProperty(ref _overshoot, NumericMath.AtLeast(value, 0f));
    }

    public float FillCoverage
    {
        get => _fillCoverage;
        set => SetProperty(ref _fillCoverage, NumericMath.Clamp01(value));
    }

    public float ShadeThreshold
    {
        get => _shadeThreshold;
        set => SetProperty(ref _shadeThreshold, NumericMath.Clamp01(value));
    }
}
