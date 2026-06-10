using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Numerics;
using System.Windows.Input;
using STFU.Assets;
using STFU.Common.Math;
using STFU.Common.Primitives;
using STFU.Engine.Commands;
using STFU.Mesh;
using STFU.NPR.Commands;
using STFU.NPR.Composition;
using STFU.UI.Bridge.Binding;
using STFU.UI.Bridge.Session;

namespace STFU.UI.Bridge.Scene;

public sealed class ScenePanelViewModel : BindableObject
{
    private readonly UiEngineSession _session;
    private EntityListItem? _selectedEntity;
    private bool _isRefreshing;

    public ScenePanelViewModel(UiEngineSession session)
    {
        _session = session;
        RoleOptions = ["Foreground", "Midground", "Background"];
        Widgets =
        [
            new("overview", "Scene Overview", "Entity count, mesh bindings, renderability, and selected object status.", nameof(SceneSummary)),
            new("outliner", "Entity Outliner", "Create, select, duplicate, and delete scene entities.", nameof(Entities)),
            new("selected", "Selected Entity", "Name, id, mesh source, mesh stats, and status for the active entity.", nameof(SelectedEntity)),
            new("transform", "Transform", "Position, rotation, scale, reset, normalize, and ground alignment.", nameof(SelectedTransformSummary)),
            new("mesh", "Mesh Binding", "Current mesh handle, source path, vertices, triangles, and bounds.", nameof(SelectedMeshSummary)),
            new("role", "NPR Role Routing", "Foreground, midground, and background style routing for the active preset.", nameof(RoleRoutes)),
            new("diagnostics", "Scene Diagnostics", "Warnings for missing meshes, empty geometry, zero scale, and scene readiness.", nameof(Diagnostics))
        ];
        CreateEntityCommand = new RelayCommand(CreateEntity);
        DeleteEntityCommand = new RelayCommand(DeleteSelectedEntity, () => SelectedEntity is not null);
        DuplicateEntityCommand = new RelayCommand(DuplicateSelectedEntity, () => SelectedEntity is not null);
        ResetTransformCommand = new RelayCommand(ResetSelectedTransform, () => SelectedEntity is not null);
        ResetTransformPositionCommand = new RelayCommand(ResetSelectedPosition, () => SelectedEntity is not null);
        ResetTransformRotationCommand = new RelayCommand(ResetSelectedRotation, () => SelectedEntity is not null);
        ResetTransformScaleCommand = new RelayCommand(ResetSelectedScale, () => SelectedEntity is not null);
        NormalizeSelectedCommand = new RelayCommand(NormalizeSelectedEntity, () => SelectedEntity?.Bounds.HasBounds == true);
        GroundSelectedCommand = new RelayCommand(GroundSelectedEntity, () => SelectedEntity?.Bounds.HasBounds == true);
        RefreshFromEngine();
    }

    public ObservableCollection<EntityListItem> Entities { get; } = [];

    public ObservableCollection<SceneDiagnosticItem> Diagnostics { get; } = [];

    public ObservableCollection<SceneRoleRouteItem> RoleRoutes { get; } = [];

    public ObservableCollection<SceneWidgetDescriptor> Widgets { get; }

    public ObservableCollection<string> RoleOptions { get; }

    public EntityListItem? SelectedEntity
    {
        get => _selectedEntity;
        set
        {
            if (ReferenceEquals(_selectedEntity, value))
            {
                return;
            }

            if (_selectedEntity is not null)
            {
                _selectedEntity.PropertyChanged -= OnSelectedEntityChanged;
            }

            if (!SetProperty(ref _selectedEntity, value))
            {
                return;
            }

            SubscribeToSelectedEntity(value);
            RaiseSelectionProperties();
            RefreshDiagnostics();
            NotifyCommandStates();
        }
    }

    public int EntityCount => Entities.Count;

    public int BoundMeshCount => Entities.Count(entity => entity.HasMesh);

    public int RenderableEntityCount => Entities.Count(entity => entity.IsRenderable);

    public string SceneSummary => EntityCount == 0
        ? "empty scene"
        : $"{RenderableEntityCount}/{EntityCount} renderable, {BoundMeshCount} mesh bindings";

    public string SelectedEntityLabel => SelectedEntity?.IdLabel ?? "no entity";

    public string SelectedEntityStatus => SelectedEntity?.Status ?? "no selection";

    public string SelectedMeshSummary => SelectedEntity is null
        ? "no selected mesh"
        : $"{SelectedEntity.MeshLabel}, {SelectedEntity.MeshStatsLabel}";

    public string SelectedBoundsSummary => SelectedEntity?.Bounds.Summary ?? "no selected bounds";

    public string SelectedTransformSummary => SelectedEntity?.TransformSummary ?? "no selected transform";

    public string DiagnosticsSummary => Diagnostics.Count == 0
        ? "scene ready"
        : $"{Diagnostics.Count} scene warning(s)";

    public ICommand CreateEntityCommand { get; }

    public ICommand DeleteEntityCommand { get; }

    public ICommand DuplicateEntityCommand { get; }

    public ICommand ResetTransformCommand { get; }

    public ICommand ResetTransformPositionCommand { get; }

    public ICommand ResetTransformRotationCommand { get; }

    public ICommand ResetTransformScaleCommand { get; }

    public ICommand NormalizeSelectedCommand { get; }

    public ICommand GroundSelectedCommand { get; }

    public float RotationMinimumDegrees => SceneTransformMath.RotationDegreesMin;

    public float RotationMaximumDegrees => SceneTransformMath.RotationDegreesMax;

    public float RotationIncrementDegrees => SceneTransformMath.RotationIncrementDegrees;

    public float ScaleMinimum => SceneTransformMath.ScaleMinimum;

    public float ScaleIncrement => SceneTransformMath.ScaleIncrement;

    public void CommitSelectedName()
    {
        if (SelectedEntity is null)
        {
            return;
        }

        _session.Commands.Execute(
            new RenameEntityCommand(SelectedEntity.Id, SelectedEntity.Name),
            $"RenameEntityCommand({SelectedEntity.IdLabel}, {SelectedEntity.Name})");
    }

    public void CommitSelectedTransform()
    {
        if (SelectedEntity is null)
        {
            return;
        }

        _session.Commands.Execute(
            new SetEntityTransformCommand(SelectedEntity.Id, CreateTransform(SelectedEntity)),
            $"SetEntityTransformCommand({SelectedEntity.IdLabel})");
        RaiseSelectionProperties();
        RefreshDiagnostics();
    }

    public void CommitSelectedRole()
    {
        if (SelectedEntity is null)
        {
            return;
        }

        if (Enum.TryParse<NprSceneRole>(SelectedEntity.Role, out var role))
        {
            _session.Commands.Execute(
                new SetEntityNprRoleCommand(SelectedEntity.Id, role),
                $"SetEntityNprRoleCommand({SelectedEntity.IdLabel}, {role})");
        }
    }

    public void RefreshFromEngine(EntityId? preferredSelection = null)
    {
        _isRefreshing = true;
        var previousSelection = preferredSelection ?? SelectedEntity?.Id;
        if (SelectedEntity is not null)
        {
            SelectedEntity.PropertyChanged -= OnSelectedEntityChanged;
        }

        Entities.Clear();

        foreach (var entity in _session.Engine.Scene.Entities)
        {
            var role = _session.EntityStyles.GetRole(entity.Id);
            var meshInfo = CreateMeshInfo(entity.Mesh);
            var transform = entity.Transform;
            Entities.Add(new EntityListItem(
                entity.Id,
                entity.Name,
                meshInfo.Label,
                meshInfo.SourcePath,
                meshInfo.VertexCount,
                meshInfo.TriangleCount,
                meshInfo.Status,
                role.ToString(),
                transform.Position.X,
                transform.Position.Y,
                transform.Position.Z,
                SceneTransformMath.ToDegrees(transform.Rotation.X),
                SceneTransformMath.ToDegrees(transform.Rotation.Y),
                SceneTransformMath.ToDegrees(transform.Rotation.Z),
                transform.Scale.X,
                transform.Scale.Y,
                transform.Scale.Z,
                meshInfo.Bounds));
        }

        _isRefreshing = false;
        SelectedEntity = previousSelection is { } id
            ? Entities.FirstOrDefault(item => item.Id == id) ?? Entities.FirstOrDefault()
            : Entities.FirstOrDefault();
        RefreshRoleRoutes();
        RefreshDiagnostics();
        RaiseSceneProperties();
    }

    public void RefreshRoleRoutes()
    {
        RoleRoutes.Clear();
        AddRoleRoute(NprSceneRole.Foreground);
        AddRoleRoute(NprSceneRole.Midground);
        AddRoleRoute(NprSceneRole.Background);
        OnPropertyChanged(nameof(RoleRoutes));
    }

    private void CreateEntity()
    {
        var name = $"Entity {Entities.Count + 1}";
        _session.Commands.Execute(new CreateEntityCommand(name), $"CreateEntityCommand(\"{name}\")");
        RefreshFromEngine();
        SelectedEntity = Entities.LastOrDefault();
    }

    private void DeleteSelectedEntity()
    {
        if (SelectedEntity is null)
        {
            return;
        }

        var deleted = SelectedEntity;
        _session.Commands.Execute(new DeleteEntityCommand(deleted.Id), $"DeleteEntityCommand({deleted.IdLabel})");
        RefreshFromEngine();
    }

    private void DuplicateSelectedEntity()
    {
        if (SelectedEntity is null)
        {
            return;
        }

        var copyName = SelectedEntity.Name + " Copy";
        _session.Commands.Execute(
            new DuplicateEntityCommand(SelectedEntity.Id, copyName),
            $"DuplicateEntityCommand({SelectedEntity.IdLabel}, {copyName})");
        RefreshFromEngine();
        SelectedEntity = Entities.LastOrDefault();
    }

    private void ResetSelectedTransform()
    {
        if (SelectedEntity is null)
        {
            return;
        }

        ApplySelectedTransform(Transform3D.Identity, "Reset entity transform");
    }

    private void ResetSelectedPosition()
    {
        if (SelectedEntity is null)
        {
            return;
        }

        ApplySelectedTransform(
            new Transform3D(
                Vector3.Zero,
                new Vector3(
                    SceneTransformMath.ToRadians(SelectedEntity.RotationX),
                    SceneTransformMath.ToRadians(SelectedEntity.RotationY),
                    SceneTransformMath.ToRadians(SelectedEntity.RotationZ)),
                new Vector3(SelectedEntity.ScaleX, SelectedEntity.ScaleY, SelectedEntity.ScaleZ)),
            "Reset entity position");
    }

    private void ResetSelectedRotation()
    {
        if (SelectedEntity is null)
        {
            return;
        }

        ApplySelectedTransform(
            new Transform3D(
                new Vector3(SelectedEntity.PositionX, SelectedEntity.PositionY, SelectedEntity.PositionZ),
                Vector3.Zero,
                new Vector3(SelectedEntity.ScaleX, SelectedEntity.ScaleY, SelectedEntity.ScaleZ)),
            "Reset entity rotation");
    }

    private void ResetSelectedScale()
    {
        if (SelectedEntity is null)
        {
            return;
        }

        ApplySelectedTransform(
            new Transform3D(
                new Vector3(SelectedEntity.PositionX, SelectedEntity.PositionY, SelectedEntity.PositionZ),
                new Vector3(
                    SceneTransformMath.ToRadians(SelectedEntity.RotationX),
                    SceneTransformMath.ToRadians(SelectedEntity.RotationY),
                    SceneTransformMath.ToRadians(SelectedEntity.RotationZ)),
                Vector3.One),
            "Reset entity scale");
    }

    private void NormalizeSelectedEntity()
    {
        if (SelectedEntity?.Bounds is not { HasBounds: true } bounds)
        {
            return;
        }

        var largestDimension = bounds.LargestDimension;
        var scale = largestDimension > 1e-6f ? 1.8f / largestDimension : 1f;
        var transform = new Transform3D(
            new Vector3(-bounds.CenterX * scale, -bounds.CenterY * scale, -bounds.CenterZ * scale),
            Vector3.Zero,
            new Vector3(scale, scale, scale));
        ApplySelectedTransform(transform, "Normalize selected entity");
    }

    private void GroundSelectedEntity()
    {
        if (SelectedEntity?.Bounds is not { HasBounds: true } bounds)
        {
            return;
        }

        var item = SelectedEntity;
        var groundY = bounds.MinY * item.ScaleY + item.PositionY;
        var transform = new Transform3D(
            new Vector3(item.PositionX, item.PositionY - groundY, item.PositionZ),
            new Vector3(
                SceneTransformMath.ToRadians(item.RotationX),
                SceneTransformMath.ToRadians(item.RotationY),
                SceneTransformMath.ToRadians(item.RotationZ)),
            new Vector3(item.ScaleX, item.ScaleY, item.ScaleZ));
        ApplySelectedTransform(transform, "Ground selected entity");
    }

    private void ApplySelectedTransform(Transform3D transform, string label)
    {
        if (SelectedEntity is null)
        {
            return;
        }

        _session.Commands.Execute(new SetEntityTransformCommand(SelectedEntity.Id, transform), label);
        RefreshFromEngine(SelectedEntity.Id);
    }

    private MeshInfo CreateMeshInfo(MeshHandle meshHandle)
    {
        if (meshHandle.Value == 0)
        {
            return new MeshInfo("no mesh", string.Empty, 0, 0, "Missing mesh", SceneMeshBoundsInfo.Empty);
        }

        if (!TryGetMeshEntry(meshHandle, out var entry))
        {
            return new MeshInfo($"MeshHandle({meshHandle.Value})", string.Empty, 0, 0, "Mesh handle not found", SceneMeshBoundsInfo.Empty);
        }

        var vertexCount = entry.Mesh.Vertices.Count;
        var triangleCount = entry.Mesh.Triangles.Count;
        var status = vertexCount > 0 && triangleCount > 0
            ? "Renderable"
            : "Empty mesh";
        return new MeshInfo(
            $"MeshHandle({entry.Handle.Value})",
            entry.Path,
            vertexCount,
            triangleCount,
            status,
            BuildBounds(entry.Mesh));
    }

    private bool TryGetMeshEntry(MeshHandle meshHandle, out AssetMeshEntry entry)
    {
        foreach (var candidate in _session.Assets.MeshEntries)
        {
            if (candidate.Handle == meshHandle)
            {
                entry = candidate;
                return true;
            }
        }

        entry = default!;
        return false;
    }

    private static SceneMeshBoundsInfo BuildBounds(MeshData mesh)
    {
        if (mesh.Vertices.Count == 0)
        {
            return SceneMeshBoundsInfo.Empty;
        }

        var min = mesh.Vertices[0].Position;
        var max = mesh.Vertices[0].Position;
        for (var index = 1; index < mesh.Vertices.Count; index++)
        {
            var position = mesh.Vertices[index].Position;
            min = Vector3.Min(min, position);
            max = Vector3.Max(max, position);
        }

        return new SceneMeshBoundsInfo(true, min.X, min.Y, min.Z, max.X, max.Y, max.Z);
    }

    private void RefreshDiagnostics()
    {
        Diagnostics.Clear();
        if (Entities.Count == 0)
        {
            Diagnostics.Add(new SceneDiagnosticItem("Warning", "Scene is empty", "Create or load an entity before rendering."));
        }

        foreach (var entity in Entities)
        {
            if (!entity.HasMesh)
            {
                Diagnostics.Add(new SceneDiagnosticItem("Warning", "Entity has no mesh", entity.Name));
            }
            else if (!entity.IsRenderable)
            {
                Diagnostics.Add(new SceneDiagnosticItem("Warning", "Entity mesh is not renderable", $"{entity.Name}: {entity.MeshStatsLabel}"));
            }

            if (MathF.Abs(entity.ScaleX) <= 1e-6f || MathF.Abs(entity.ScaleY) <= 1e-6f || MathF.Abs(entity.ScaleZ) <= 1e-6f)
            {
                Diagnostics.Add(new SceneDiagnosticItem("Error", "Entity has zero scale", entity.Name));
            }
        }

        if (SelectedEntity is null && Entities.Count > 0)
        {
            Diagnostics.Add(new SceneDiagnosticItem("Info", "No entity selected", "Select an entity to edit transform, mesh, and role."));
        }

        OnPropertyChanged(nameof(DiagnosticsSummary));
        OnPropertyChanged(nameof(Diagnostics));
    }

    private void AddRoleRoute(NprSceneRole role)
    {
        var style = _session.ActivePreset.ActiveStyleSet.GetRoleStyle(role);
        RoleRoutes.Add(new SceneRoleRouteItem(
            role.ToString(),
            style.Layers.Count,
            style.Layers.Count(layer => layer.Visible),
            style.OpacityScale,
            style.StrokeScale,
            style.DetailScale,
            style.ToneScale,
            style.HatchScale));
    }

    private static Transform3D CreateTransform(EntityListItem entity)
    {
        return new Transform3D(
            new Vector3(entity.PositionX, entity.PositionY, entity.PositionZ),
            new Vector3(    
                SceneTransformMath.ToRadians(entity.RotationX),
                SceneTransformMath.ToRadians(entity.RotationY),
                SceneTransformMath.ToRadians(entity.RotationZ)),
            new Vector3(entity.ScaleX, entity.ScaleY, entity.ScaleZ));
    }

    private void SubscribeToSelectedEntity(EntityListItem? entity)
    {
        if (entity is null)
        {
            return;
        }

        entity.PropertyChanged -= OnSelectedEntityChanged;
        entity.PropertyChanged += OnSelectedEntityChanged;
    }

    private void OnSelectedEntityChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isRefreshing)
        {
            return;
        }

        switch (e.PropertyName)
        {
            case nameof(EntityListItem.Name):
                CommitSelectedName();
                break;
            case nameof(EntityListItem.PositionX):
            case nameof(EntityListItem.PositionY):
            case nameof(EntityListItem.PositionZ):
            case nameof(EntityListItem.RotationX):
            case nameof(EntityListItem.RotationY):
            case nameof(EntityListItem.RotationZ):
            case nameof(EntityListItem.ScaleX):
            case nameof(EntityListItem.ScaleY):
            case nameof(EntityListItem.ScaleZ):
                CommitSelectedTransform();
                break;
            case nameof(EntityListItem.Role):
                CommitSelectedRole();
                break;
        }
    }

    private void RaiseSelectionProperties()
    {
        OnPropertyChanged(nameof(SelectedEntityLabel));
        OnPropertyChanged(nameof(SelectedEntityStatus));
        OnPropertyChanged(nameof(SelectedMeshSummary));
        OnPropertyChanged(nameof(SelectedBoundsSummary));
        OnPropertyChanged(nameof(SelectedTransformSummary));
    }

    private void RaiseSceneProperties()
    {
        OnPropertyChanged(nameof(EntityCount));
        OnPropertyChanged(nameof(BoundMeshCount));
        OnPropertyChanged(nameof(RenderableEntityCount));
        OnPropertyChanged(nameof(SceneSummary));
        RaiseSelectionProperties();
    }

    private void NotifyCommandStates()
    {
        if (DeleteEntityCommand is RelayCommand delete)
        {
            delete.NotifyCanExecuteChanged();
        }

        if (DuplicateEntityCommand is RelayCommand duplicate)
        {
            duplicate.NotifyCanExecuteChanged();
        }

        if (ResetTransformCommand is RelayCommand reset)
        {
            reset.NotifyCanExecuteChanged();
        }

        if (ResetTransformPositionCommand is RelayCommand resetPosition)
        {
            resetPosition.NotifyCanExecuteChanged();
        }

        if (ResetTransformRotationCommand is RelayCommand resetRotation)
        {
            resetRotation.NotifyCanExecuteChanged();
        }

        if (ResetTransformScaleCommand is RelayCommand resetScale)
        {
            resetScale.NotifyCanExecuteChanged();
        }

        if (NormalizeSelectedCommand is RelayCommand normalize)
        {
            normalize.NotifyCanExecuteChanged();
        }

        if (GroundSelectedCommand is RelayCommand ground)
        {
            ground.NotifyCanExecuteChanged();
        }
    }

    private readonly record struct MeshInfo(
        string Label,
        string SourcePath,
        int VertexCount,
        int TriangleCount,
        string Status,
        SceneMeshBoundsInfo Bounds);
}
