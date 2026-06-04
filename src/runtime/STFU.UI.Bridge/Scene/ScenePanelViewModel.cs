using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Numerics;
using System.Windows.Input;
using STFU.Common.Primitives;
using STFU.Engine.Commands;
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
        CreateEntityCommand = new RelayCommand(CreateEntity);
        DeleteEntityCommand = new RelayCommand(DeleteSelectedEntity, () => SelectedEntity is not null);
        RefreshFromEngine();
    }

    public ObservableCollection<EntityListItem> Entities { get; } = [];

    public ObservableCollection<string> RoleOptions { get; }

    public EntityListItem? SelectedEntity
    {
        get => _selectedEntity;
        set
        {
            if (!SetProperty(ref _selectedEntity, value))
            {
                return;
            }

            OnPropertyChanged(nameof(SceneSummary));
            OnPropertyChanged(nameof(SelectedEntityLabel));
            if (DeleteEntityCommand is RelayCommand command)
            {
                command.NotifyCanExecuteChanged();
            }

            SubscribeToSelectedEntity(value);
        }
    }

    public string SceneSummary => SelectedEntity is null ? "none selected" : "1 selected";

    public string SelectedEntityLabel => SelectedEntity?.IdLabel ?? "no entity";

    public ICommand CreateEntityCommand { get; }

    public ICommand DeleteEntityCommand { get; }

    public void CommitSelectedPosition()
    {
        if (SelectedEntity is null)
        {
            return;
        }

        _session.Commands.Execute(
            new SetEntityPositionCommand(
                SelectedEntity.Id,
                new Vector3(SelectedEntity.PositionX, SelectedEntity.PositionY, SelectedEntity.PositionZ)),
            $"SetEntityPositionCommand({SelectedEntity.IdLabel})");
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
        if (SelectedEntity is not null)
        {
            SelectedEntity.PropertyChanged -= OnSelectedEntityChanged;
        }

        Entities.Clear();

        foreach (var entity in _session.Engine.Scene.Entities)
        {
            var role = _session.EntityStyles.GetRole(entity.Id);
            var item = new EntityListItem(
                entity.Id,
                entity.Name,
                entity.Mesh.Value == 0 ? "no mesh" : $"MeshHandle({entity.Mesh.Value})",
                role.ToString(),
                entity.Transform.Position.X,
                entity.Transform.Position.Y,
                entity.Transform.Position.Z);
            Entities.Add(item);
        }

        SelectedEntity = preferredSelection is { } id
            ? Entities.FirstOrDefault(item => item.Id == id) ?? Entities.FirstOrDefault()
            : Entities.FirstOrDefault();
        _isRefreshing = false;
        SubscribeToSelectedEntity(SelectedEntity);
    }

    private void CreateEntity()
    {
        var name = $"Entity {Entities.Count + 1}";
        _session.Commands.Execute(new CreateEntityCommand(name), $"CreateEntityCommand(\"{name}\")");
        RefreshFromEngine();
    }

    private void DeleteSelectedEntity()
    {
        if (SelectedEntity is null)
        {
            return;
        }

        _session.Commands.Execute(new DeleteEntityCommand(SelectedEntity.Id), $"DeleteEntityCommand({SelectedEntity.IdLabel})");
        RefreshFromEngine();
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
            case nameof(EntityListItem.PositionX):
            case nameof(EntityListItem.PositionY):
            case nameof(EntityListItem.PositionZ):
                CommitSelectedPosition();
                break;
            case nameof(EntityListItem.Role):
                CommitSelectedRole();
                break;
        }
    }
}
