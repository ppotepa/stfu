namespace STFU.Rendering.DirectX.Diagnostics;

public sealed class DirectXRenderCounters
{
    public int StrokeInstances { get; set; }

    public int StrokeInstancesBuilt
    {
        get => StrokeInstances;
        set => StrokeInstances = value;
    }

    public int StrokeInstanceUploads { get; set; }

    public int StrokeInstanceBufferRecreates { get; set; }

    public int ToneSurfaceUploads { get; set; }

    public int ToneSurfaceCacheHits { get; set; }

    public int ToneSurfaceCacheMisses { get; set; }

    public int MeshBufferUploads { get; set; }

    public int MeshBufferCacheHits { get; set; }

    public int Readbacks { get; set; }

    public int DebugOverlayPaths { get; set; }

    public long UploadedBytes { get; set; }
}
