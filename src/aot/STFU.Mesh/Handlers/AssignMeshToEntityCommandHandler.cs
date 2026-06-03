using STFU.Mesh.Commands;
using STFU.Engine.Scenes;
using STFU.Messaging.Commands;

namespace STFU.Mesh.Handlers;

public sealed class AssignMeshToEntityCommandHandler : ICommandHandler<AssignMeshToEntityCommand>
{
    private readonly Scene _scene;

    public AssignMeshToEntityCommandHandler(Scene scene)
    {
        _scene = scene;
    }

    public void Handle(AssignMeshToEntityCommand command)
    {
        if (_scene.TryGetEntity(command.EntityId, out var entity))
        {
            entity.Mesh = command.Mesh;
        }
    }
}
