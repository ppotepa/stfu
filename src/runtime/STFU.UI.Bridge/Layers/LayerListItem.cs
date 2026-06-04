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
        set => SetProperty(ref _opacity, Math.Clamp(value, 0f, 1f));
    }

    public float Density
    {
        get => _density;
        set => SetProperty(ref _density, Math.Max(0f, value));
    }

    public float BaseThickness
    {
        get => _baseThickness;
        set => SetProperty(ref _baseThickness, Math.Max(0.1f, value));
    }

    public float ThicknessVariation
    {
        get => _thicknessVariation;
        set => SetProperty(ref _thicknessVariation, Math.Clamp(value, 0f, 1f));
    }

    public float EndpointJitter
    {
        get => _endpointJitter;
        set => SetProperty(ref _endpointJitter, Math.Max(0f, value));
    }

    public float Overshoot
    {
        get => _overshoot;
        set => SetProperty(ref _overshoot, Math.Max(0f, value));
    }

    public float FillCoverage
    {
        get => _fillCoverage;
        set => SetProperty(ref _fillCoverage, Math.Clamp(value, 0f, 1f));
    }

    public float ShadeThreshold
    {
        get => _shadeThreshold;
        set => SetProperty(ref _shadeThreshold, Math.Clamp(value, 0f, 1f));
    }
}
