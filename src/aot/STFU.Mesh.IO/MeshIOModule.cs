using STFU.Engine.Composition;
using STFU.Mesh.Loading;
using STFU.MeshIO.Formats;

namespace STFU.MeshIO;

public sealed class MeshIOModule : IEngineModule
{
    public void Register(EngineModuleContext context)
    {
        context.Services.AddSingleton<IMeshLoader<string>>(new ObjMeshLoader());
    }
}
