using STFU.Abstractions.Modules;
using STFU.Rendering.Abstractions.Backend;
using STFU.Rendering.Abstractions.Execution;
using STFU.Rendering.Abstractions.Surfaces;
using STFU.Rendering.Cpu.Backend;

namespace STFU.Rendering.Cpu;

public sealed class CpuRenderingModule : IEngineModule
{
    public void Register(IModuleContext context)
    {
        var surfacePool = new PixelSurfacePool(maxRetainedSurfaces: 4);
        var backend = new FullCpuRenderBackend(surfacePool);
        var renderer = new FullCpuNprRenderer(backend);
        var worker = new FullCpuRenderWorker(renderer);
        var scheduler = new FullCpuRenderScheduler(worker);

        context.Services.AddSingleton(surfacePool);
        context.Services.AddSingleton(backend);
        context.Services.AddSingleton<ICpuRenderBackend>(backend);
        context.Services.AddSingleton<INprRenderer>(renderer);
        context.Services.AddSingleton(worker);
        context.Services.AddSingleton<INprRenderScheduler>(scheduler);
    }
}
