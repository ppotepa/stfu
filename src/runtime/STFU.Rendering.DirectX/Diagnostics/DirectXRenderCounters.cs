namespace STFU.Rendering.DirectX.Diagnostics;

public sealed class DirectXRenderCounters
{
    public int StrokeInstances { get; set; }

    public int ToneSurfaceUploads { get; set; }

    public int DebugOverlayPaths { get; set; }

    public long UploadedBytes { get; set; }
}
