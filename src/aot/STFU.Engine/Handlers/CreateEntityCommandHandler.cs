using STFU.Engine.Commands;
using STFU.Engine.Scenes;
using STFU.Messaging.Commands;

namespace STFU.Engine.Handlers;

public sealed class CreateEntityCommandHandler : ICommandHandler<CreateEntityCommand>
{
    private readonly Scene _scene;

    public CreateEntityCommandHandler(Scene scene)
    {
        _scene = scene;
    }

    public void Handle(CreateEntityCommand command)
    {
        _scene.CreateEntity(command.Name);
    }
}
