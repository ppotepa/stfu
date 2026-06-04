using System.Collections.ObjectModel;
using System.Windows.Input;
using STFU.UI.Bridge.Binding;

namespace STFU.UI.Bridge.Inspector;

public sealed class InspectorViewModel : BindableObject
{
    private InspectorTab _activeTab = InspectorTab.Load;

    public InspectorViewModel()
    {
        Tabs =
        [
            new(InspectorTab.Load, "LOAD", "\uE8B7", "Load", "Assets, mesh handles, and import entry points.",
            [
                new("Asset Registry", "Loaded meshes and handles."),
                new("Load Mesh", "Open or reload source geometry."),
                new("Assign Mesh", "Bind selected mesh to selected entity.")
            ]),
            new(InspectorTab.Scene, "SCENE", "\uECA5", "Scene", "Entities, transforms, roles, and scene graph basics.",
            [
                new("Entities", "Create, select, and remove scene entities."),
                new("Transform", "Position, rotation, and scale."),
                new("Role", "Foreground, midground, background style routing.")
            ]),
            new(InspectorTab.General, "GENERAL", "\uE713", "General", "Preset, pipeline, seed, and viewport mode.",
            [
                new("Preset", "Active preset metadata and pipeline provider."),
                new("Render Mode", "Mesh, NPR, and future viewport modes."),
                new("Session", "Seed, deterministic mode, and basic workspace state.")
            ]),
            new(InspectorTab.Camera, "CAMERA", "\uE722", "Camera", "CameraState and viewport camera commands.",
            [
                new("Position", "Camera position and target."),
                new("Projection", "FOV, near, far, and projection options."),
                new("Controls", "Orbit, pan, reset, and frame model.")
            ]),
            new(InspectorTab.Npr, "NPR", "\uE9F5", "NPR", "Pipeline settings and draw-flow controls.",
            [
                new("Pipeline", "Active NPR pipeline and steps."),
                new("Features", "Silhouette, boundary, crease, visibility."),
                new("Draw", "Draw progress, auto draw, and projection parameters.")
            ]),
            new(InspectorTab.Style, "STYLE", "\uE790", "Style", "Stroke style, humanization, and preset overrides.",
            [
                new("Stroke", "Line width, pressure, jitter, and medium."),
                new("Humanization", "Variation, overshoot, and noise profile."),
                new("Overrides", "Per-style and future per-entity overrides.")
            ]),
            new(InspectorTab.Layers, "LAYERS", "\uE8A9", "Layers", "Style layers, marks, fills, tones, and routing.",
            [
                new("Stack", "Layer order, visibility, blend, and opacity."),
                new("Routing", "NprStrokeIntent to layer mapping."),
                new("Output", "Strokes, fills, shading, and tone channels.")
            ]),
            new(InspectorTab.Debug, "DEBUG", "\uE9D9", "Debug", "Counters, traces, overlays, and determinism.",
            [
                new("Counters", "Graph, path, stroke, and layer counters."),
                new("Trace", "Pipeline step timing and before/after counts."),
                new("Overlays", "Debug viewport overlays and diagnostics.")
            ])
        ];
        UpdateActiveTabs();

        SetTabCommand = new RelayCommand(parameter =>
        {
            if (parameter is InspectorTab tab)
            {
                ActiveTab = tab;
            }
            else if (parameter is InspectorTabItem item)
            {
                ActiveTab = item.Id;
            }
            else if (parameter is string text && Enum.TryParse<InspectorTab>(text, ignoreCase: true, out var parsed))
            {
                ActiveTab = parsed;
            }
        });
    }

    public ObservableCollection<InspectorTabItem> Tabs { get; }

    public ICommand SetTabCommand { get; }

    public InspectorTab ActiveTab
    {
        get => _activeTab;
        set
        {
            if (!SetProperty(ref _activeTab, value))
            {
                return;
            }

            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(Subtitle));
            OnPropertyChanged(nameof(Icon));
            OnPropertyChanged(nameof(Sections));
            OnPropertyChanged(nameof(IsLoadActive));
            OnPropertyChanged(nameof(IsBoilerplateActive));
            UpdateActiveTabs();
        }
    }

    public string Title => ActiveItem.Title;

    public string Subtitle => ActiveItem.Subtitle;

    public string Icon => ActiveItem.Icon;

    public IReadOnlyList<InspectorSectionItem> Sections => ActiveItem.Sections;

    public bool IsLoadActive => ActiveTab == InspectorTab.Load;

    public bool IsBoilerplateActive => !IsLoadActive;

    private InspectorTabItem ActiveItem => Tabs.First(item => item.Id == ActiveTab);

    private void UpdateActiveTabs()
    {
        foreach (var tab in Tabs)
        {
            tab.IsActive = tab.Id == ActiveTab;
        }
    }
}
