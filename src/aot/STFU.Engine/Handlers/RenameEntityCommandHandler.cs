using STFU.Engine.Commands;
using STFU.Engine.Scenes;
using STFU.Messaging.Commands;

namespace STFU.Engine.Handlers;

public sealed class RenameEntityCommandHandler : ICommandHandler<RenameEntityCommand>
{
    private readonly Scene _scene;

    public RenameEntityCommandHandler(Scene scene)
    {
        _scene = scene;
    }

    public void Handle(RenameEntityCommand command)
    {
        if (!_scene.TryGetEntity(command.EntityId, out var entity))
        {
            return;
        }

        var name = string.IsNullOrWhiteSpace(command.Name)
            ? entity.Name
            : command.Name.Trim();
        if (name.Length == 0)
        {
            return;
        }

        entity.Name = name;
    }
}
