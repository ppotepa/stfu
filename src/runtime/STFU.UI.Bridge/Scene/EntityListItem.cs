using STFU.Common.Primitives;
using STFU.UI.Bridge.Binding;

namespace STFU.UI.Bridge.Scene;

public sealed class EntityListItem : BindableObject
{
    private string _role;
    private float _positionX;
    private float _positionY;
    private float _positionZ;

    public EntityListItem(EntityId id, string name, string meshLabel, string role, float positionX, float positionY, float positionZ)
    {
        Id = id;
        Name = name;
        MeshLabel = meshLabel;
        _role = role;
        _positionX = positionX;
        _positionY = positionY;
        _positionZ = positionZ;
    }

    public EntityId Id { get; }

    public string Name { get; }

    public string MeshLabel { get; }

    public string Role
    {
        get => _role;
        set => SetProperty(ref _role, value);
    }

    public float PositionX
    {
        get => _positionX;
        set => SetProperty(ref _positionX, value);
    }

    public float PositionY
    {
        get => _positionY;
        set => SetProperty(ref _positionY, value);
    }

    public float PositionZ
    {
        get => _positionZ;
        set => SetProperty(ref _positionZ, value);
    }

    public string IdLabel => $"EntityId({Id.Value})";
}
