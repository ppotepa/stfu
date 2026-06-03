using STFU.Engine.Composition;
using STFU.Mesh.Commands;
using STFU.Mesh.Handlers;

namespace STFU.Mesh;

public sealed class MeshModule : IEngineModule
{
    public void Register(EngineModuleContext context)
    {
        context.Services.AddSingleton(new MeshFactory());

        context.Commands
            .Register(new AssignMeshToEntityCommandHandler(context.Scene));
    }
}
