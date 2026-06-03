using STFU.Common.Primitives;
using STFU.Engine.Entities;

namespace STFU.Engine.Scenes;

public sealed class Scene
{
    private readonly List<Entity> _entities = [];
    private int _nextEntityId;

    public IReadOnlyList<Entity> Entities => _entities;

    public Entity CreateEntity(string name)
    {
        var entity = new Entity(new EntityId(++_nextEntityId), name);
        _entities.Add(entity);
        return entity;
    }

    public bool TryGetEntity(EntityId id, out Entity entity)
    {
        foreach (var candidate in _entities)
        {
            if (candidate.Id == id)
            {
                entity = candidate;
                return true;
            }
        }

        entity = default!;
        return false;
    }

    public bool DeleteEntity(EntityId id)
    {
        for (var index = 0; index < _entities.Count; index++)
        {
            if (_entities[index].Id == id)
            {
                _entities.RemoveAt(index);
                return true;
            }
        }

        return false;
    }
}
