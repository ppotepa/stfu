using STFU.Parallelism;

namespace STFU.Rendering.Abstractions.Requests;

public sealed record NprFrameBudget(
    int TargetFps = 60,
    int MaxWorkerThreads = 0,
    int MinimumWorkerThreads = 1,
    int MaximumWorkerThreads = 0,
    bool AllowContinuousRendering = true,
    bool AllowDroppingOldFrames = true,
    bool EnableTileParallelism = true,
    int TileSize = 32,
    bool RequireGpuReadback = false,
    bool AllowGpuReadback = true,
    bool PreferGpuPresentation = true,
    bool EnableGpuDebugLayer = false,
    bool EnableGpuTiming = true,
    bool AllowGpuVisibilityFallback = true,
    float GpuVisibilityRequiredMatchRatio = 0.995f,
    WorkerBudgetMode WorkerBudgetMode = WorkerBudgetMode.Performance)
{
    public WorkerBudgetRequest ToWorkerBudgetRequest()
    {
        return new WorkerBudgetRequest(
            Mode: WorkerBudgetMode,
            ExplicitWorkerCount: MaxWorkerThreads,
            MinimumWorkers: MinimumWorkerThreads,
            MaximumWorkers: MaximumWorkerThreads);
    }

    public int ResolveWorkerCount()
    {
        return WorkerBudget.Resolve(ToWorkerBudgetRequest());
    }
}
