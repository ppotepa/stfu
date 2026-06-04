using STFU.Common.Primitives;

namespace STFU.NPR.Composition;

public sealed class NprEntityStyleRegistry
{
    private readonly Dictionary<EntityId, NprSceneRole> _roles = new();

    public NprSceneRole DefaultRole { get; set; } = NprSceneRole.Foreground;

    public void SetRole(EntityId entityId, NprSceneRole role)
    {
        _roles[entityId] = role;
    }

    public NprSceneRole GetRole(EntityId entityId)
    {
        return _roles.TryGetValue(entityId, out var role)
            ? role
            : DefaultRole;
    }
}
