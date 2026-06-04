using STFU.Logging;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using static Vortice.Direct3D11.D3D11;
using static Vortice.DXGI.DXGI;

namespace STFU.Rendering.DirectX.Device;

public sealed record DirectXDeviceCreateResult(
    bool Success,
    DirectXDevice? Device,
    string? Error);

public static class DirectXDeviceFactory
{
    public static DirectXDeviceCreateResult TryCreate(DirectXDeviceOptions options)
    {
        if (!OperatingSystem.IsWindows())
        {
            StfuLog.Write(StfuLogDomain.DirectX, "device.skipped", "DirectX backend is Windows-only.");
            return new(false, null, "DirectX backend is Windows-only.");
        }

        try
        {
            var factory = CreateDXGIFactory2<IDXGIFactory4>(false);
            var adapter = SelectAdapter(factory, options.PreferWarp);
            var flags = DeviceCreationFlags.BgraSupport;
            if (options.EnableDebugLayer)
            {
                flags |= DeviceCreationFlags.Debug;
            }

            var levels = new[]
            {
                FeatureLevel.Level_11_1,
                FeatureLevel.Level_11_0,
                FeatureLevel.Level_10_1,
                FeatureLevel.Level_10_0
            };

            D3D11CreateDevice(
                adapter,
                DriverType.Unknown,
                flags,
                levels,
                out var device,
                out var featureLevel,
                out var context).CheckError();

            var description = adapter.Description1;
            var support = new DirectXFeatureSupport(
                description.Description,
                featureLevel.ToString(),
                SupportsBgra: true,
                SupportsCompute: featureLevel >= FeatureLevel.Level_11_0,
                SupportsTimestampQueries: true);
            StfuLog.Write(
                StfuLogDomain.DirectX,
                "device.created",
                description.Description,
                properties: new Dictionary<string, object?>
                {
                    ["featureLevel"] = featureLevel,
                    ["preferWarp"] = options.PreferWarp,
                    ["debugLayer"] = options.EnableDebugLayer
                });

            return new(true, new DirectXDevice(factory, adapter, device, context, support), null);
        }
        catch (Exception ex)
        {
            StfuLog.Write(StfuLogDomain.DirectX, "device.failed", ex.Message, StfuLogLevel.Error, exception: ex);
            return new(false, null, ex.Message);
        }
    }

    private static IDXGIAdapter1 SelectAdapter(IDXGIFactory4 factory, bool preferWarp)
    {
        if (preferWarp)
        {
            return factory.EnumWarpAdapter<IDXGIAdapter1>();
        }

        for (uint index = 0; factory.EnumAdapters1(index, out var adapter).Success; index++)
        {
            var description = adapter.Description1;
            if ((description.Flags & AdapterFlags.Software) == 0)
            {
                StfuLog.Write(StfuLogDomain.DirectX, "adapter.selected", description.Description);
                return adapter;
            }

            adapter.Dispose();
        }

        var warp = factory.EnumWarpAdapter<IDXGIAdapter1>();
        StfuLog.Write(StfuLogDomain.DirectX, "adapter.selected", warp.Description1.Description);
        return warp;
    }
}
