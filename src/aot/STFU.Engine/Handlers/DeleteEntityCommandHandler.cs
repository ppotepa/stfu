using STFU.Engine.Commands;
using STFU.Engine.Scenes;
using STFU.Messaging.Commands;

namespace STFU.Engine.Handlers;

public sealed class DeleteEntityCommandHandler : ICommandHandler<DeleteEntityCommand>
{
    private readonly Scene _scene;

    public DeleteEntityCommandHandler(Scene scene)
    {
        _scene = scene;
    }

    public void Handle(DeleteEntityCommand command)
    {
        _scene.DeleteEntity(command.EntityId);
    }
}
