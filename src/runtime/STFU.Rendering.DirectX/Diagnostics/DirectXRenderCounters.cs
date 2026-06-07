namespace STFU.Rendering.DirectX.Diagnostics;

public sealed class DirectXRenderCounters
{
    public long StrokeInstances { get; set; }

    public long StrokeInstancesBuilt
    {
        get => StrokeInstances;
        set => StrokeInstances = value;
    }

    public long StrokeInstanceUploads { get; set; }

    public long StrokeInstanceBufferRecreates { get; set; }

    public long ToneSurfaceUploads { get; set; }

    public long ToneSurfaceCacheHits { get; set; }

    public long ToneSurfaceCacheMisses { get; set; }

    public long MeshBufferUploads { get; set; }

    public long MeshBufferCacheHits { get; set; }

    public long Readbacks { get; set; }

    public int DebugOverlayPaths { get; set; }

    public long UploadedBytes { get; set; }
}
