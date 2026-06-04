namespace STFU.Rendering.DirectX.Device;

public sealed record DirectXDeviceOptions(
    bool EnableDebugLayer = false,
    bool PreferWarp = false,
    bool RequireBgraSupport = true)
{
    public static DirectXDeviceOptions Default { get; } = new();
}
