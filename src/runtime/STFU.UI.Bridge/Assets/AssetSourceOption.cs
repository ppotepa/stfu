using STFU.UI.Bridge.Binding;

namespace STFU.UI.Bridge.Assets;

public sealed class AssetSourceOption : BindableObject, IAssetSource
{
    private bool _isSelected;

    public AssetSourceOption(string id, string displayName)
    {
        Id = id;
        DisplayName = displayName;
    }

    public string Id { get; }

    public string DisplayName { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
