using STFU.Engine.Commands;
using STFU.Engine.Scenes;
using STFU.Messaging.Commands;

namespace STFU.Engine.Handlers;

public sealed class SetEntityPositionCommandHandler : ICommandHandler<SetEntityPositionCommand>
{
    private readonly Scene _scene;

    public SetEntityPositionCommandHandler(Scene scene)
    {
        _scene = scene;
    }

    public void Handle(SetEntityPositionCommand command)
    {
        if (_scene.TryGetEntity(command.EntityId, out var entity))
        {
            entity.Position = command.Position;
        }
    }
}
