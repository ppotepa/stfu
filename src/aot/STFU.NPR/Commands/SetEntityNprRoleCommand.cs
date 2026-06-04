using STFU.Common.Primitives;
using STFU.Messaging.Commands;
using STFU.NPR.Composition;

namespace STFU.NPR.Commands;

public readonly record struct SetEntityNprRoleCommand(
    EntityId EntityId,
    NprSceneRole Role) : ICommand;
