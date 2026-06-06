using STFU.Common.Math;
using STFU.UI.Bridge.Binding;

namespace STFU.UI.Bridge.Layers;

public sealed class IntentRouteItem : BindableObject
{
    private bool _enabled;
    private int _count;

    public IntentRouteItem(string name, bool enabled, int count)
    {
        Name = name;
        _enabled = enabled;
        _count = count;
    }

    public string Name { get; }

    public bool Enabled
    {
        get => _enabled;
        set => SetProperty(ref _enabled, value);
    }

    public int Count
    {
        get => _count;
        set => SetProperty(ref _count, NumericMath.AtLeast(value, 0));
    }
}
