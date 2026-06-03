using System.Numerics;
using STFU.Common.Primitives;
using STFU.Messaging.Commands;

namespace STFU.Engine.Commands;

public readonly record struct SetEntityPositionCommand(
    EntityId EntityId,
    Vector3 Position) : ICommand;
