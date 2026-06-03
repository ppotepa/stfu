using System.Numerics;
using STFU.Common.Math;
using STFU.Common.Primitives;

namespace STFU.Engine.Entities;

public sealed class Entity
{
    public Entity(EntityId id, string name)
    {
        Id = id;
        Name = name;
        Transform = Transform3D.Identity;
    }

    public EntityId Id { get; }

    public string Name { get; set; }

    public MeshHandle Mesh { get; set; } = MeshHandle.None;

    public Transform3D Transform { get; set; }

    public Vector3 Position
    {
        get => Transform.Position;
        set => Transform = Transform.WithPosition(value);
    }
}
