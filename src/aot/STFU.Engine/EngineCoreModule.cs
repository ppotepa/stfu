using STFU.Engine.Commands;
using STFU.Engine.Composition;
using STFU.Engine.Handlers;

namespace STFU.Engine;

public sealed class EngineCoreModule : IEngineModule
{
    public void Register(EngineModuleContext context)
    {
        context.Commands
            .Register(new CreateEntityCommandHandler(context.Scene))
            .Register(new DeleteEntityCommandHandler(context.Scene))
            .Register(new SetEntityPositionCommandHandler(context.Scene));
    }
}
