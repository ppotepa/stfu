using STFU.Abstractions.Modules;
using STFU.Camera.Commands;
using STFU.Camera.Handlers;

namespace STFU.Camera;

public sealed class CameraModule : IEngineModule
{
    public void Register(IModuleContext context)
    {
        var state = new CameraRig();

        context.Services.AddSingleton(state);
        context.Commands.Register(new SetCameraCommandHandler(state));
        context.Commands.Register(new OrbitCameraCommandHandler(state));
        context.Commands.Register(new PanCameraCommandHandler(state));
        context.Commands.Register(new AdjustCameraFovCommandHandler(state));
    }
}
