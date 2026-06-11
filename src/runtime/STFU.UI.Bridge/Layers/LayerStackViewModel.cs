using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using STFU.Common.Math;
using STFU.NPR.Composition;
using STFU.Strokes;
using STFU.UI.Bridge.Binding;
using STFU.UI.Bridge.Session;

namespace STFU.UI.Bridge.Layers;

public sealed class LayerStackViewModel : BindableObject
{
    private readonly UiEngineSession _session;
    private readonly Dictionary<string, HashSet<string>> _layerIntents = new(StringComparer.OrdinalIgnoreCase);
    private StrokeFrame? _intentCountFrame;
    private readonly Dictionary<string, int> _intentCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _intentLayerCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _intentUnlayeredCounts = new(StringComparer.OrdinalIgnoreCase);
    private LayerListItem? _selectedLayer;
    private string _newLayerType = "Strokes";
    private int _nextLayerId = 1;
    private bool _isRefreshing;
    private bool _isRefreshingIntents;

    public LayerStackViewModel(UiEngineSession session)
    {
        _session = session;
        AddLayerCommand = new RelayCommand(AddLayer);
        DuplicateLayerCommand = new RelayCommand(DuplicateSelectedLayer, () => SelectedLayer is not null);
        DeleteLayerCommand = new RelayCommand(DeleteSelectedLayer, () => SelectedLayer is { Locked: false });
        RefreshFromEngine();
    }

    public ObservableCollection<LayerListItem> Layers { get; } = [];

    public ObservableCollection<LayerPreviewItem> ActiveLayerPreview { get; } = [];

    public ObservableCollection<LayerPreviewItem> CompositeLayerPreview { get; } = [];

    public ObservableCollection<IntentRouteItem> Intents { get; } =
    [
        new("Silhouette", true, 0),
        new("Boundary", true, 0),
        new("Feature", true, 0),
        new("Crease", true, 0),
        new("SurfaceFlow", false, 0),
        new("Hatch", true, 0),
        new("Accent", true, 0),
        new("Fill", true, 0),
        new("Tones", true, 0)
    ];

    public ObservableCollection<string> LayerTypes { get; } = ["Strokes", "Fill", "Shading", "Tones"];

    public ObservableCollection<string> BlendModes { get; } = ["Normal", "Multiply"];

    public string NewLayerType
    {
        get => _newLayerType;
        set => SetProperty(ref _newLayerType, value);
    }

    public LayerListItem? SelectedLayer
    {
        get => _selectedLayer;
        set
        {
            if (!SetProperty(ref _selectedLayer, value))
            {
                return;
            }

            OnPropertyChanged(nameof(ActiveLayerId));
            OnPropertyChanged(nameof(SelectedLayerSummary));
            RefreshIntentRoutesFromSelectedLayer();
            RefreshPreviews();
            NotifyCommandStates();
        }
    }

    public string ActiveLayerId => SelectedLayer?.Id ?? "no layer";

    public string SelectedLayerSummary => SelectedLayer is null
        ? "no active layer"
        : $"{SelectedLayer.Type} layer, opacity {SelectedLayer.Opacity:0.00}, density {SelectedLayer.Density:0.00}";

    public int VisibleLayerCount => Layers.Count(layer => layer.Visible);

    public int StrokeOutputCount => _session.Strokes.CurrentFrame.Segments.Count;

    public int StrokePathOutputCount => _session.Strokes.CurrentFrame.Paths.Count;

    public int ToneOutputCount => _session.NprFrames.CurrentFrame.Layers.Sum(layer => layer.Tones.Count + layer.Shading.Count);

    public string LayerStackSummary => $"{VisibleLayerCount} visible / {Layers.Count} total";

    public ICommand AddLayerCommand { get; }

    public ICommand DuplicateLayerCommand { get; }

    public ICommand DeleteLayerCommand { get; }

    public void RefreshFromEngine()
    {
        _isRefreshing = true;
        foreach (var layer in Layers)
        {
            layer.PropertyChanged -= OnLayerChanged;
        }

        Layers.Clear();
        _layerIntents.Clear();
        AddRoleLayers(NprSceneRole.Foreground, _session.ActivePreset.ActiveStyleSet.Foreground);
        AddRoleLayers(NprSceneRole.Midground, _session.ActivePreset.ActiveStyleSet.Midground);
        AddRoleLayers(NprSceneRole.Background, _session.ActivePreset.ActiveStyleSet.Background);
        _isRefreshing = false;
        SelectedLayer = Layers.FirstOrDefault();
        RaiseLayerStats();
    }

    public void RefreshRuntimeCounters()
    {
        RaiseLayerStats();
    }

    private void AddRoleLayers(NprSceneRole sceneRole, NprRoleStyle role)
    {
        foreach (var layer in role.Layers)
        {
            var item = new LayerListItem(
                layer.Id,
                layer.Name,
                sceneRole.ToString(),
                InferLayerType(layer),
                layer.Visible,
                layer.Opacity,
                InferLayerDensity(layer, role),
                layer.BlendMode.ToString(),
                locked: !_session.ActivePreset.ActivePreset.Metadata.IsEditable,
                baseThickness: InferBaseThickness(layer, role),
                thicknessVariation: _session.ActivePreset.ActiveSettings.StrokeStyle.ThicknessVariation,
                endpointJitter: _session.ActivePreset.ActiveSettings.StrokeStyle.EndpointJitter,
                overshoot: _session.ActivePreset.ActiveSettings.StrokeStyle.Overshoot,
                fillCoverage: layer.MainFill.Enabled ? layer.MainFill.Opacity : 0f,
                shadeThreshold: _session.ActivePreset.ActiveSettings.HatchShadeThreshold);
            AddLayerItem(item, InferLayerIntents(layer));
        }
    }

    private void AddLayer()
    {
        var type = string.IsNullOrWhiteSpace(NewLayerType) ? "Strokes" : NewLayerType;
        var id = $"custom-layer:{_nextLayerId++}";
        var item = new LayerListItem(
            id,
            $"{type} Layer {_nextLayerId - 1}",
            RoleForType(type),
            type,
            visible: true,
            opacity: type == "Strokes" ? 0.82f : 0.36f,
            density: type == "Strokes" ? 0.7f : 0.55f,
            blend: type == "Strokes" ? "Normal" : "Multiply",
            baseThickness: _session.ActivePreset.ActiveSettings.StrokeStyle.BaseThickness,
            thicknessVariation: _session.ActivePreset.ActiveSettings.StrokeStyle.ThicknessVariation,
            endpointJitter: _session.ActivePreset.ActiveSettings.StrokeStyle.EndpointJitter,
            overshoot: _session.ActivePreset.ActiveSettings.StrokeStyle.Overshoot,
            fillCoverage: type is "Fill" or "Tones" ? 0.35f : 0f,
            shadeThreshold: 0.58f);

        AddLayerItem(item, DefaultIntentsForType(type));
        SelectedLayer = item;
        _session.Commands.Record($"AddNprLayerCommand({item.Id}, Type={item.Type})");
        RaiseLayerStats();
    }

    private void DuplicateSelectedLayer()
    {
        if (SelectedLayer is null)
        {
            return;
        }

        var source = SelectedLayer;
        var clone = new LayerListItem(
            $"custom-layer:{_nextLayerId++}",
            $"{source.Name} Copy",
            source.Role,
            source.Type,
            source.Visible,
            source.Opacity,
            source.Density,
            source.Blend,
            locked: false,
            source.BaseThickness,
            source.ThicknessVariation,
            source.EndpointJitter,
            source.Overshoot,
            source.FillCoverage,
            source.ShadeThreshold);

        var sourceIndex = Layers.IndexOf(source);
        clone.PropertyChanged += OnLayerChanged;
        var intents = _layerIntents.TryGetValue(source.Id, out var sourceIntents)
            ? new HashSet<string>(sourceIntents, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _layerIntents[clone.Id] = intents;
        Layers.Insert(NumericMath.AtLeast(sourceIndex, 0) + 1, clone);
        SelectedLayer = clone;
        _session.Commands.Record($"DuplicateNprLayerCommand(\"{source.Id}\") -> \"{clone.Id}\"");
        RaiseLayerStats();
    }

    private void DeleteSelectedLayer()
    {
        if (SelectedLayer is null || SelectedLayer.Locked)
        {
            return;
        }

        var removed = SelectedLayer;
        removed.PropertyChanged -= OnLayerChanged;
        _layerIntents.Remove(removed.Id);
        Layers.Remove(removed);
        SelectedLayer = Layers.FirstOrDefault();
        _session.Commands.Record($"DeleteNprLayerCommand(\"{removed.Id}\")");
        RaiseLayerStats();
    }

    private void AddLayerItem(LayerListItem item, IEnumerable<string> intents)
    {
        item.PropertyChanged += OnLayerChanged;
        _layerIntents[item.Id] = new HashSet<string>(intents, StringComparer.OrdinalIgnoreCase);
        Layers.Add(item);
    }

    private void OnLayerChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isRefreshing || sender is not LayerListItem layer)
        {
            return;
        }

        if (ReferenceEquals(layer, SelectedLayer))
        {
            RefreshPreviews();
            OnPropertyChanged(nameof(SelectedLayerSummary));
        }

        if (e.PropertyName == nameof(LayerListItem.Type))
        {
            NewLayerType = layer.Type;
            if (layer.Type is "Fill" or "Shading" or "Tones" && layer.Blend == "Normal")
            {
                layer.Blend = "Multiply";
            }

            _layerIntents[layer.Id] = DefaultIntentsForType(layer.Type);
            RefreshIntentRoutesFromSelectedLayer();
        }

        _session.Commands.Record($"UpdateNprLayerCommand(\"{layer.Id}\", {e.PropertyName})");
        RaiseLayerStats();
    }

    private void RefreshIntentRoutesFromSelectedLayer()
    {
        _isRefreshingIntents = true;
        var enabled = SelectedLayer is not null && _layerIntents.TryGetValue(SelectedLayer.Id, out var intents)
            ? intents
            : [];
        var routedLayerIdsByIntent = BuildRoutedLayerIdsByIntent();
        RebuildIntentCountsIfNeeded();

        foreach (var intent in Intents)
        {
            intent.PropertyChanged -= OnIntentChanged;
            intent.Enabled = enabled.Contains(intent.Name);
            intent.Count = CountIntent(intent.Name, routedLayerIdsByIntent);
            intent.PropertyChanged += OnIntentChanged;
        }

        _isRefreshingIntents = false;
    }

    private void OnIntentChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isRefreshingIntents || e.PropertyName != nameof(IntentRouteItem.Enabled) || sender is not IntentRouteItem intent || SelectedLayer is null)
        {
            return;
        }

        if (!_layerIntents.TryGetValue(SelectedLayer.Id, out var enabled))
        {
            enabled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _layerIntents[SelectedLayer.Id] = enabled;
        }

        if (intent.Enabled)
        {
            enabled.Add(intent.Name);
        }
        else
        {
            enabled.Remove(intent.Name);
        }

        _session.Commands.Record($"RouteNprStrokeIntentCommand(\"{SelectedLayer.Id}\", {intent.Name}, {(intent.Enabled ? "on" : "off")})");
        RefreshPreviews();
    }

    private void RefreshPreviews()
    {
        ActiveLayerPreview.Clear();
        CompositeLayerPreview.Clear();

        if (SelectedLayer is not null)
        {
            foreach (var mark in BuildPreviewMarks(SelectedLayer, composite: false))
            {
                ActiveLayerPreview.Add(mark);
            }
        }

        foreach (var layer in Layers.Where(layer => layer.Visible))
        {
            foreach (var mark in BuildPreviewMarks(layer, composite: true))
            {
                CompositeLayerPreview.Add(mark);
            }
        }
    }

    private static IEnumerable<LayerPreviewItem> BuildPreviewMarks(LayerListItem layer, bool composite)
    {
        var opacity = composite ? NumericMath.Clamp(layer.Opacity * 0.78d, 0.05d, 1.0d) : layer.Opacity;
        var color = layer.Type switch
        {
            "Fill" => "#A6ADA2",
            "Tones" => "#747D70",
            "Shading" => "#77746B",
            _ => "#23201C"
        };

        if (layer.Type is "Fill" or "Tones")
        {
            yield return new LayerPreviewItem(layer.Type, 10, 12, 120, 48, opacity * NumericMath.AtLeast((double)layer.FillCoverage, 0.1d), color, 0);
            yield break;
        }

        if (layer.Type == "Shading")
        {
            yield return new LayerPreviewItem(layer.Type, 12, 18, 118, NumericMath.AtLeast(layer.BaseThickness * 1.2d, 2d), opacity * layer.Density, color, -8);
            yield return new LayerPreviewItem(layer.Type, 18, 38, 106, NumericMath.AtLeast((double)layer.BaseThickness, 2d), opacity * layer.Density * 0.8d, color, -8);
            yield return new LayerPreviewItem(layer.Type, 24, 58, 92, NumericMath.AtLeast(layer.BaseThickness * 0.8d, 2d), opacity * layer.Density * 0.6d, color, -8);
            yield break;
        }

        yield return new LayerPreviewItem(layer.Type, 12, 20, 112, NumericMath.AtLeast(layer.BaseThickness * 2.1d, 2d), opacity, color, -5);
        yield return new LayerPreviewItem(layer.Type, 20, 42, 84, NumericMath.AtLeast(layer.BaseThickness * 1.45d, 2d), opacity * 0.82d, color, 4);
        yield return new LayerPreviewItem(layer.Type, 50, 30, 62, NumericMath.AtLeast(layer.BaseThickness * 1.05d, 2d), opacity * 0.7d, color, -18);
    }

    private void RaiseLayerStats()
    {
        OnPropertyChanged(nameof(VisibleLayerCount));
        OnPropertyChanged(nameof(StrokeOutputCount));
        OnPropertyChanged(nameof(ToneOutputCount));
        OnPropertyChanged(nameof(LayerStackSummary));
        OnPropertyChanged(nameof(SelectedLayerSummary));
        RefreshIntentRoutesFromSelectedLayer();
        RefreshPreviews();
        NotifyCommandStates();
    }

    private void NotifyCommandStates()
    {
        if (DuplicateLayerCommand is RelayCommand duplicate)
        {
            duplicate.NotifyCanExecuteChanged();
        }

        if (DeleteLayerCommand is RelayCommand delete)
        {
            delete.NotifyCanExecuteChanged();
        }
    }

    private Dictionary<string, HashSet<string>> BuildRoutedLayerIdsByIntent()
    {
        var routedLayerIdsByIntent = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var (layerId, intents) in _layerIntents)
        {
            foreach (var intent in intents)
            {
                if (!routedLayerIdsByIntent.TryGetValue(intent, out var layerIds))
                {
                    layerIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    routedLayerIdsByIntent[intent] = layerIds;
                }

                layerIds.Add(layerId);
            }
        }

        return routedLayerIdsByIntent;
    }

    private void RebuildIntentCountsIfNeeded()
    {
        var frame = _session.Strokes.CurrentFrame;
        if (ReferenceEquals(_intentCountFrame, frame))
        {
            return;
        }

        _intentCountFrame = frame;
        _intentCounts.Clear();
        _intentLayerCounts.Clear();
        _intentUnlayeredCounts.Clear();

        var segments = frame.Segments;
        for (var i = 0; i < segments.Count; i++)
        {
            AccumulateIntentCount(segments[i].Metadata);
        }
    }

    private void AccumulateIntentCount(StrokeMetadata? metadata)
    {
        var intent = metadata?.Intent ?? "Unknown";
        Increment(_intentCounts, intent);

        var layer = metadata?.Layer;
        if (string.IsNullOrWhiteSpace(layer))
        {
            Increment(_intentUnlayeredCounts, intent);
            return;
        }

        Increment(_intentLayerCounts, intent + "|" + layer);
    }

    private static void Increment(Dictionary<string, int> dictionary, string key)
    {
        dictionary.TryGetValue(key, out var value);
        dictionary[key] = value + 1;
    }

    private int CountIntent(string intentName, Dictionary<string, HashSet<string>> routedLayerIdsByIntent)
    {
        RebuildIntentCountsIfNeeded();
        if (!routedLayerIdsByIntent.TryGetValue(intentName, out var layerIds) || layerIds.Count == 0)
        {
            _intentUnlayeredCounts.TryGetValue(intentName, out var unlayered);
            return unlayered;
        }

        var total = 0;
        foreach (var layerId in layerIds)
        {
            _intentLayerCounts.TryGetValue(intentName + "|" + layerId, out var count);
            total += count;
        }

        return total;
    }

    private static string InferLayerType(NprLayerStyle layer)
    {
        if (layer.MainFill.Enabled)
        {
            return "Fill";
        }

        if (layer.Hatching.Enabled)
        {
            return "Shading";
        }

        return "Strokes";
    }

    private static float InferLayerDensity(NprLayerStyle layer, NprRoleStyle role)
    {
        if (layer.Hatching.Enabled)
        {
            return NumericMath.AtLeast(layer.Hatching.DensityScale * role.HatchScale, 0f);
        }

        if (layer.MainFill.Enabled)
        {
            return NumericMath.AtLeast(layer.MainFill.ShadeInfluence * role.ToneScale, 0f);
        }

        return NumericMath.AtLeast(role.DetailScale, 0f);
    }

    private float InferBaseThickness(NprLayerStyle layer, NprRoleStyle role)
    {
        var lineWidth = _session.ActivePreset.ActiveSettings.DefaultDrawing.LineWidth > 0f
            ? _session.ActivePreset.ActiveSettings.DefaultDrawing.LineWidth
            : _session.ActivePreset.ActiveSettings.StrokeStyle.BaseThickness;
        var channelScale = NumericMath.AtLeast(
            layer.Contour.ThicknessScale,
            NumericMath.AtLeast(layer.Crease.ThicknessScale, layer.Accent.ThicknessScale));
        return NumericMath.AtLeast(lineWidth * role.StrokeScale * NumericMath.AtLeast(channelScale, 0.2f), 0.2f);
    }

    private static IEnumerable<string> InferLayerIntents(NprLayerStyle layer)
    {
        if (layer.Contour.Enabled)
        {
            yield return "Silhouette";
            yield return "Boundary";
        }

        if (layer.Crease.Enabled)
        {
            yield return "Feature";
            yield return "Crease";
        }

        if (layer.Accent.Enabled)
        {
            yield return "Accent";
        }

        if (layer.Hatching.Enabled)
        {
            yield return "Hatch";
            yield return "SurfaceFlow";
            yield return "Tones";
        }

        if (layer.MainFill.Enabled)
        {
            yield return "Fill";
            yield return "Tones";
        }
    }

    private static HashSet<string> DefaultIntentsForType(string type)
    {
        return type switch
        {
            "Fill" => new HashSet<string>(["Fill", "Tones"], StringComparer.OrdinalIgnoreCase),
            "Shading" => new HashSet<string>(["Hatch", "SurfaceFlow", "Tones"], StringComparer.OrdinalIgnoreCase),
            "Tones" => new HashSet<string>(["Tones", "Fill"], StringComparer.OrdinalIgnoreCase),
            _ => new HashSet<string>(["Accent"], StringComparer.OrdinalIgnoreCase)
        };
    }

    private static string RoleForType(string type)
    {
        return type switch
        {
            "Fill" or "Tones" => "Background",
            "Shading" => "Midground",
            _ => "Foreground"
        };
    }
}
