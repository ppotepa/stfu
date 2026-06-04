using System.Collections.ObjectModel;
using System.Windows.Input;
using STFU.Assets;
using STFU.Common.Primitives;
using STFU.Engine.Commands;
using STFU.Mesh.Commands;
using STFU.Mesh.Loading;
using STFU.UI.Bridge.Binding;
using STFU.UI.Bridge.Scene;
using STFU.UI.Bridge.Session;
using STFU.Viewport;

namespace STFU.UI.Bridge.Assets;

public sealed class AssetPanelViewModel : BindableObject
{
    private readonly UiEngineSession _session;
    private readonly ScenePanelViewModel _scene;
    private AssetListItem? _selectedAsset;
    private AssetSourceOption? _selectedSource;
    private AssetRecentItem? _selectedRecent;
    private bool _normalizeSize = true;
    private bool _centerPivot = true;
    private bool _loadAnimations = true;

    public AssetPanelViewModel(UiEngineSession session, ScenePanelViewModel scene)
    {
        _session = session;
        _scene = scene;
        SourceOptions =
        [
            new("hard-drive", "HARD DRIVE"),
            new("public-domain", "PUBLIC DOMAIN")
        ];
        Recents =
        [
            new("Suzanne", ResolveAssetPath("suzanne.obj"), "Project Assets", ".obj"),
            new("Walking", Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "assets", "walking.fbx")), "Hard Drive", ".fbx")
        ];
        SelectSourceCommand = new RelayCommand(parameter =>
        {
            if (parameter is AssetSourceOption option)
            {
                SelectedSource = option;
            }
        });
        SelectRecentCommand = new RelayCommand(parameter =>
        {
            if (parameter is AssetRecentItem item)
            {
                SelectedRecent = item;
            }
        });
        LoadAssetCommand = new RelayCommand(LoadAsset);
        ReloadAssetCommand = new RelayCommand(ReloadSelectedAsset, () => SelectedAsset is not null);
        AssignMeshCommand = new RelayCommand(AssignSelectedMesh, () => SelectedAsset is not null && _scene.SelectedEntity is not null);
        SelectedSource = SourceOptions[0];
        SelectedRecent = Recents[0];
        RefreshFromEngine();
    }

    public ObservableCollection<AssetSourceOption> SourceOptions { get; }

    public ObservableCollection<AssetRecentItem> Recents { get; }

    public ObservableCollection<AssetListItem> Assets { get; } = [];

    public AssetSourceOption? SelectedSource
    {
        get => _selectedSource;
        set
        {
            if (!SetProperty(ref _selectedSource, value))
            {
                return;
            }

            foreach (var option in SourceOptions)
            {
                option.IsSelected = ReferenceEquals(option, value);
            }

            OnPropertyChanged(nameof(SourceStatus));
        }
    }

    public AssetRecentItem? SelectedRecent
    {
        get => _selectedRecent;
        set
        {
            if (!SetProperty(ref _selectedRecent, value))
            {
                return;
            }

            foreach (var item in Recents)
            {
                item.IsSelected = ReferenceEquals(item, value);
            }

            OnPropertyChanged(nameof(SelectedAssetName));
            OnPropertyChanged(nameof(SelectedAssetPath));
            OnPropertyChanged(nameof(SelectedAssetFormat));
            OnPropertyChanged(nameof(SelectedAssetStatus));
        }
    }

    public AssetListItem? SelectedAsset
    {
        get => _selectedAsset;
        set
        {
            if (!SetProperty(ref _selectedAsset, value))
            {
                return;
            }

            OnPropertyChanged(nameof(LoaderStatus));
            OnPropertyChanged(nameof(SelectedAssetName));
            OnPropertyChanged(nameof(SelectedAssetPath));
            OnPropertyChanged(nameof(SelectedAssetFormat));
            OnPropertyChanged(nameof(SelectedAssetStatus));
            OnPropertyChanged(nameof(SelectedAssetMetadata));
            if (ReloadAssetCommand is RelayCommand reload)
            {
                reload.NotifyCanExecuteChanged();
            }

            if (AssignMeshCommand is RelayCommand assign)
            {
                assign.NotifyCanExecuteChanged();
            }
        }
    }

    public string LoaderStatus => SelectedAsset is null ? "ObjMeshLoader ready" : $"{SelectedAsset.Loader} ready";

    public string SourceStatus => SelectedSource?.DisplayName ?? "HARD DRIVE";

    public string SelectedAssetName => SelectedRecent?.Name ?? SelectedAsset?.Id ?? "none";

    public string SelectedAssetPath => SelectedRecent?.Path ?? SelectedAsset?.Path ?? "no asset selected";

    public string SelectedAssetFormat => SelectedRecent?.Format ?? GetFormat(SelectedAsset?.Path);

    public string SelectedAssetStatus => File.Exists(SelectedAssetPath) ? "ready" : "missing";

    public string SelectedAssetMetadata => SelectedAsset is null
        ? "metadata after load"
        : $"{SelectedAsset.VertexCount} vertices / {SelectedAsset.TriangleCount} triangles";

    public bool NormalizeSize
    {
        get => _normalizeSize;
        set => SetProperty(ref _normalizeSize, value);
    }

    public bool CenterPivot
    {
        get => _centerPivot;
        set => SetProperty(ref _centerPivot, value);
    }

    public bool LoadAnimations
    {
        get => _loadAnimations;
        set => SetProperty(ref _loadAnimations, value);
    }

    public ICommand SelectSourceCommand { get; }

    public ICommand SelectRecentCommand { get; }

    public ICommand LoadAssetCommand { get; }

    public ICommand ReloadAssetCommand { get; }

    public ICommand AssignMeshCommand { get; }

    public void RefreshFromEngine()
    {
        var selectedPath = SelectedAsset?.Path;
        Assets.Clear();

        foreach (var entry in _session.Assets.MeshEntries.OrderBy(entry => entry.Handle.Value))
        {
            Assets.Add(CreateAssetItem(entry));
        }

        SelectedAsset = Assets.FirstOrDefault(item => string.Equals(item.Path, selectedPath, StringComparison.OrdinalIgnoreCase))
            ?? Assets.FirstOrDefault();
        OnPropertyChanged(nameof(LoaderStatus));
    }

    public void SelectAssetCandidate(string path, string source)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var fullPath = Path.GetFullPath(path);
        var item = Recents.FirstOrDefault(candidate => string.Equals(candidate.Path, fullPath, StringComparison.OrdinalIgnoreCase));
        if (item is null)
        {
            item = new AssetRecentItem(
                Path.GetFileNameWithoutExtension(fullPath),
                fullPath,
                source,
                GetFormat(fullPath));
            Recents.Insert(0, item);
        }

        SelectedRecent = item;
        _session.Commands.Record($"Selected asset candidate: {fullPath}");
    }

    public void SelectSource(string id)
    {
        var source = SourceOptions.FirstOrDefault(option => string.Equals(option.Id, id, StringComparison.OrdinalIgnoreCase));
        if (source is not null)
        {
            SelectedSource = source;
            _session.Commands.Record($"AssetSource selected: {source.DisplayName}");
        }
    }

    private void LoadAsset()
    {
        var path = SelectedRecent?.Path ?? SelectedAsset?.Path;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            path = ResolveAssetPath("suzanne.obj");
        }

        var handle = LoadMesh(path, "LoadMeshCommand");
        if (handle.Value == 0)
        {
            return;
        }

        var entity = EnsureTargetEntity();
        _session.Commands.Execute(
            new AssignMeshToEntityCommand(entity.Id, handle),
            $"LOAD -> AssignMeshToEntityCommand({entity.IdLabel}, MeshHandle({handle.Value}))");
        _scene.RefreshFromEngine(entity.Id);
        _session.Workspace.Viewport.RenderMode = ViewportRenderMode.Mesh;
    }

    private void ReloadSelectedAsset()
    {
        if (SelectedAsset is null)
        {
            return;
        }

        LoadMesh(SelectedAsset.Path, "ReloadMeshCommand");
    }

    private void AssignSelectedMesh()
    {
        if (SelectedAsset is null || _scene.SelectedEntity is null)
        {
            return;
        }

        var handle = new MeshHandle(SelectedAsset.Handle);
        _session.Commands.Execute(
            new AssignMeshToEntityCommand(_scene.SelectedEntity.Id, handle),
            $"AssignMeshToEntityCommand({_scene.SelectedEntity.IdLabel}, MeshHandle({handle.Value}))");
        _scene.RefreshFromEngine(_scene.SelectedEntity.Id);
    }

    private MeshHandle LoadMesh(string path, string commandName)
    {
        var fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            _session.Commands.Record($"{commandName} failed: {fullPath} was not found");
            return default;
        }

        var loader = _session.Engine.Registry.GetRequired<IMeshLoader<string>>();
        var mesh = _session.MeshFactory.Load(fullPath, loader);
        var handle = _session.Assets.AddMesh(fullPath, mesh);
        RefreshFromEngine();
        SelectedAsset = Assets.FirstOrDefault(item => item.Handle == handle.Value);
        _session.Commands.Record($"{commandName}(\"{fullPath}\") -> MeshHandle({handle.Value})");
        AddRecent(fullPath);
        return handle;
    }

    private EntityListItem EnsureTargetEntity()
    {
        if (_scene.SelectedEntity is not null)
        {
            return _scene.SelectedEntity;
        }

        var name = Path.GetFileNameWithoutExtension(SelectedAssetPath);
        if (string.IsNullOrWhiteSpace(name))
        {
            name = $"Asset {Assets.Count + 1}";
        }

        _session.Commands.Execute(new CreateEntityCommand(name), $"CreateEntityCommand(\"{name}\")");
        _scene.RefreshFromEngine();
        return _scene.Entities.LastOrDefault()
            ?? throw new InvalidOperationException("LOAD could not create a target entity.");
    }

    private void AddRecent(string path)
    {
        var existing = Recents.FirstOrDefault(item => string.Equals(item.Path, path, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            SelectedRecent = existing;
            return;
        }

        var item = new AssetRecentItem(
            Path.GetFileNameWithoutExtension(path),
            path,
            SelectedSource?.DisplayName ?? "HARD DRIVE",
            GetFormat(path));
        Recents.Insert(0, item);
        SelectedRecent = item;
    }

    private static AssetListItem CreateAssetItem(AssetMeshEntry entry)
    {
        var id = Path.GetFileNameWithoutExtension(entry.Path);
        return new AssetListItem(
            string.IsNullOrWhiteSpace(id) ? $"mesh-{entry.Handle.Value}" : id,
            entry.Path,
            entry.Handle.Value,
            entry.Mesh.Vertices.Count,
            entry.Mesh.Triangles.Count,
            "ObjMeshLoader",
            "Loaded");
    }

    private static string ResolveAssetPath(string fileName)
    {
        foreach (var root in EnumerateAssetRoots())
        {
            var path = Path.GetFullPath(Path.Combine(root, "assets", fileName));
            if (File.Exists(path))
            {
                return path;
            }
        }

        return Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "assets", fileName));
    }

    private static string GetFormat(string? path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? "unknown"
            : Path.GetExtension(path).ToLowerInvariant();
    }

    private static IEnumerable<string> EnumerateAssetRoots()
    {
        yield return Environment.CurrentDirectory;

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            yield return directory.FullName;
            directory = directory.Parent;
        }
    }
}
