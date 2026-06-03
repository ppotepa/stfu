using STFU.Common.Primitives;
using STFU.Messaging.Commands;

namespace STFU.Mesh.Commands;

public readonly record struct AssignMeshToEntityCommand(
    EntityId EntityId,
    MeshHandle Mesh) : ICommand;
