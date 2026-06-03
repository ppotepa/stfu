using STFU.Engine.Composition;
using STFU.Projection.Commands;
using STFU.Projection.Handlers;

namespace STFU.Projection;

public sealed class ProjectionModule : IEngineModule
{
    public void Register(EngineModuleContext context)
    {
        var state = new ProjectionState();

        context.Services.AddSingleton(state);
        context.Commands.Register(new SetCameraCommandHandler(state));
    }
}
