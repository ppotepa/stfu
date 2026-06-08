using STFU.Abstractions.Modules;
using STFU.Engine.Commands;
using STFU.Engine.Handlers;
using STFU.Engine.Scenes;

namespace STFU.Engine;

public sealed class EngineCoreModule : IEngineModule
{
    public void Register(IModuleContext context)
    {
        var scene = context.Services.GetRequired<Scene>();

        context.Commands
            .Register(new CreateEntityCommandHandler(scene))
            .Register(new DeleteEntityCommandHandler(scene))
            .Register(new RenameEntityCommandHandler(scene))
            .Register(new DuplicateEntityCommandHandler(scene))
            .Register(new SetEntityPositionCommandHandler(scene))
            .Register(new SetEntityTransformCommandHandler(scene));
    }
}
