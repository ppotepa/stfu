using STFU.Abstractions.Modules;
using STFU.Logging;
using STFU.Rendering.Abstractions.Backend;
using STFU.Rendering.Abstractions.Execution;
using STFU.Rendering.Abstractions.Surfaces;
using STFU.Rendering.DirectX.Backend;
using STFU.Rendering.DirectX.Device;

namespace STFU.Rendering.DirectX;

public sealed class DirectXRenderingModule : IEngineModule
{
    public void Register(IModuleContext context)
    {
        if (!OperatingSystem.IsWindows())
        {
            StfuLog.Write(StfuLogDomain.RenderGpu, "module.skipped", "DirectX backend skipped: Windows-only.");
            return;
        }

        if (!context.Services.TryGet<ICpuRenderBackend>(out var cpuBackend))
        {
            StfuLog.Write(StfuLogDomain.RenderGpu, "module.skipped", "DirectX backend skipped: CPU fallback backend is unavailable.");
            return;
        }

        var deviceResult = DirectXDeviceFactory.TryCreate(DirectXDeviceOptions.Default);
        if (!deviceResult.Success || deviceResult.Device is null)
        {
            StfuLog.Write(
                StfuLogDomain.RenderGpu,
                "module.unavailable",
                deviceResult.Error ?? "DirectX device could not be created.",
                StfuLogLevel.Warning);
            return;
        }

        var device = deviceResult.Device;
        var surfacePool = context.Services.TryGet<PixelSurfacePool>(out var existingPool)
            ? existingPool
            : new PixelSurfacePool(maxRetainedSurfaces: 4);
        var gpuBackend = new DirectXRenderBackend(device, cpuBackend, surfacePool);
        var renderer = new DirectXNprRenderer(cpuBackend, gpuBackend);
        var worker = new DirectXRenderWorker(renderer);
        var scheduler = new DirectXRenderScheduler(worker);

        context.Services.AddSingleton(surfacePool);
        context.Services.AddSingleton(device);
        context.Services.AddSingleton<IGpuRenderBackend>(gpuBackend);
        context.Services.AddSingleton(renderer);
        context.Services.AddSingleton(worker);
        context.Services.AddSingleton<INprRenderer>(renderer);
        context.Services.AddSingleton<INprRenderScheduler>(scheduler);
        StfuLog.Write(
            StfuLogDomain.RenderGpu,
            "module.registered",
            device.Support.AdapterName,
            properties: new Dictionary<string, object?>
            {
                ["featureLevel"] = device.Support.FeatureLevel,
                ["compute"] = device.Support.SupportsCompute,
                ["timestamps"] = device.Support.SupportsTimestampQueries
            });
    }
}
