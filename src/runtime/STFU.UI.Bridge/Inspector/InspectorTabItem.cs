using STFU.UI.Bridge.Binding;

namespace STFU.UI.Bridge.Inspector;

public sealed class InspectorTabItem : BindableObject
{
    private bool _isActive;

    public InspectorTabItem(
        InspectorTab id,
        string label,
        string icon,
        string title,
        string subtitle,
        IReadOnlyList<InspectorSectionItem> sections)
    {
        Id = id;
        Label = label;
        Icon = icon;
        Title = title;
        Subtitle = subtitle;
        Sections = sections;
    }

    public InspectorTab Id { get; }

    public string Label { get; }

    public string Icon { get; }

    public string Title { get; }

    public string Subtitle { get; }

    public IReadOnlyList<InspectorSectionItem> Sections { get; }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }
}

public sealed record InspectorSectionItem(
    string Title,
    string Description);
