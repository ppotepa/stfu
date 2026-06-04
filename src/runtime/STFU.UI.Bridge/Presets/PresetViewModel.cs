using System.Collections.ObjectModel;
using STFU.NPR.Composition;
using STFU.NPR.Temporal;
using STFU.UI.Bridge.Binding;
using STFU.UI.Bridge.Session;

namespace STFU.UI.Bridge.Presets;

public sealed class PresetViewModel : BindableObject
{
    private readonly NprPresetRegistry _registry;
    private readonly ActiveNprPresetState _activePreset;
    private readonly FrameHistoryState _frameHistory;
    private string _activePresetId = string.Empty;
    private string _activePresetName = string.Empty;
    private string _activePresetDescription = string.Empty;
    private string _activePipelineId = string.Empty;
    private string _activePresetProvider = string.Empty;
    private bool _activePresetIsEditable;
    private PresetListItem? _activePresetItem;

    public PresetViewModel(
        NprPresetRegistry registry,
        ActiveNprPresetState activePreset,
        FrameHistoryState frameHistory,
        UiCommandBus commands)
    {
        _registry = registry;
        _activePreset = activePreset;
        _frameHistory = frameHistory;
        Commands = commands;

        foreach (var preset in registry.Presets.OrderBy(preset => preset.Metadata.Id))
        {
            Presets.Add(new PresetListItem(
                preset.Metadata.Id,
                preset.Metadata.Name,
                preset.Metadata.Description,
                preset.Metadata.IsEditable,
                preset.PipelineId,
                preset.Metadata.Author));
        }

        RefreshFromEngine();
    }

    public UiCommandBus Commands { get; }

    public ObservableCollection<PresetListItem> Presets { get; } = [];

    public string ActivePresetId
    {
        get => _activePresetId;
        set => ApplyPreset(value);
    }

    public PresetListItem? ActivePresetItem
    {
        get => _activePresetItem;
        set
        {
            if (value is null)
            {
                return;
            }

            ApplyPreset(value.Id);
        }
    }

    public string ActivePresetName
    {
        get => _activePresetName;
        private set => SetProperty(ref _activePresetName, value);
    }

    public string ActivePresetDescription
    {
        get => _activePresetDescription;
        private set => SetProperty(ref _activePresetDescription, value);
    }

    public string ActivePipelineId
    {
        get => _activePipelineId;
        private set => SetProperty(ref _activePipelineId, value);
    }

    public string ActivePresetProvider
    {
        get => _activePresetProvider;
        private set => SetProperty(ref _activePresetProvider, value);
    }

    public bool ActivePresetIsEditable
    {
        get => _activePresetIsEditable;
        private set => SetProperty(ref _activePresetIsEditable, value);
    }

    public void ApplyPreset(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || string.Equals(id, _activePresetId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!_registry.TryGet(id, out _))
        {
            return;
        }

        _activePreset.ApplyPreset(id);
        _frameHistory.Reset();
        Commands.Record($"ActiveNprPresetState.ApplyPreset({id})");
        RefreshFromEngine();
    }

    public void RefreshFromEngine()
    {
        var preset = _activePreset.ActivePreset;
        SetProperty(ref _activePresetId, preset.Metadata.Id, nameof(ActivePresetId));
        ActivePresetName = preset.Metadata.Name;
        ActivePresetDescription = preset.Metadata.Description;
        ActivePipelineId = preset.PipelineId;
        ActivePresetProvider = $"STFU.NPR.Pipeline.{preset.PipelineId}";
        ActivePresetIsEditable = preset.Metadata.IsEditable;
        SetProperty(
            ref _activePresetItem,
            Presets.FirstOrDefault(item => string.Equals(item.Id, preset.Metadata.Id, StringComparison.OrdinalIgnoreCase)),
            nameof(ActivePresetItem));
    }
}
