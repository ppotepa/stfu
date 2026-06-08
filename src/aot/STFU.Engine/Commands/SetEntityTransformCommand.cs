using STFU.Common.Math;
using STFU.Common.Primitives;
using STFU.Messaging.Commands;

namespace STFU.Engine.Commands;

public readonly record struct SetEntityTransformCommand(
    EntityId EntityId,
    Transform3D Transform) : ICommand;
