namespace STFU.Rendering.Abstractions.Requests;

public sealed record NprFrameBudget(
    int TargetFps = 60,
    int MaxWorkerThreads = 0,
    bool AllowContinuousRendering = true,
    bool AllowDroppingOldFrames = true,
    bool EnableTileParallelism = true,
    int TileSize = 32,
    bool RequireGpuReadback = false,
    bool AllowGpuReadback = true,
    bool PreferGpuPresentation = true,
    bool EnableGpuDebugLayer = false,
    bool EnableGpuTiming = true)
{
    public int ResolveWorkerCount()
    {
        if (MaxWorkerThreads > 0)
        {
            return Math.Max(1, MaxWorkerThreads);
        }

        return Math.Max(1, Environment.ProcessorCount - 2);
    }
}
