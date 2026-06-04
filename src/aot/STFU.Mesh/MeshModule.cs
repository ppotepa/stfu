using STFU.Abstractions.Modules;
using STFU.Engine.Scenes;
using STFU.Mesh.Commands;
using STFU.Mesh.Handlers;

namespace STFU.Mesh;

public sealed class MeshModule : IEngineModule
{
    public void Register(IModuleContext context)
    {
        var scene = context.Services.GetRequired<Scene>();

        context.Services.AddSingleton(new MeshFactory());

        context.Commands
            .Register(new AssignMeshToEntityCommandHandler(scene));
    }
}
