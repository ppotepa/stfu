using STFU.Engine.Commands;
using STFU.Engine.Scenes;
using STFU.Messaging.Commands;

namespace STFU.Engine.Handlers;

public sealed class SetEntityTransformCommandHandler : ICommandHandler<SetEntityTransformCommand>
{
    private readonly Scene _scene;

    public SetEntityTransformCommandHandler(Scene scene)
    {
        _scene = scene;
    }

    public void Handle(SetEntityTransformCommand command)
    {
        if (_scene.TryGetEntity(command.EntityId, out var entity))
        {
            entity.Transform = command.Transform;
        }
    }
}
