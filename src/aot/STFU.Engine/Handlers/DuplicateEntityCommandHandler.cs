using STFU.Engine.Commands;
using STFU.Engine.Scenes;
using STFU.Messaging.Commands;

namespace STFU.Engine.Handlers;

public sealed class DuplicateEntityCommandHandler : ICommandHandler<DuplicateEntityCommand>
{
    private readonly Scene _scene;

    public DuplicateEntityCommandHandler(Scene scene)
    {
        _scene = scene;
    }

    public void Handle(DuplicateEntityCommand command)
    {
        if (!_scene.TryGetEntity(command.SourceEntityId, out var source))
        {
            return;
        }

        var name = string.IsNullOrWhiteSpace(command.Name)
            ? source.Name + " Copy"
            : command.Name.Trim();
        var copy = _scene.CreateEntity(name);
        copy.Mesh = source.Mesh;
        copy.Transform = source.Transform;
    }
}
