namespace STFU.Rendering.Abstractions.Requests;

public sealed record NprDiagnosticsOptions(
    bool EnablePassTimings = true,
    bool EnableStepAllocationTracking = false,
    bool EnableDetailedStepNotes = false,
    bool EnableMemoryLogs = false,
    bool EnablePixelHash = false,
    bool EnableFrameHash = false,
    bool EnableRangeTimings = false)
{
    public static NprDiagnosticsOptions Default { get; } = new();

    public static NprDiagnosticsOptions InteractiveViewport { get; } = new(
        EnablePassTimings: false,
        EnableStepAllocationTracking: false,
        EnableDetailedStepNotes: false,
        EnableMemoryLogs: false,
        EnablePixelHash: false,
        EnableFrameHash: false,
        EnableRangeTimings: false);

    public static NprDiagnosticsOptions InteractiveViewportTimings { get; } = new(
        EnablePassTimings: true,
        EnableStepAllocationTracking: false,
        EnableDetailedStepNotes: false,
        EnableMemoryLogs: false,
        EnablePixelHash: false,
        EnableFrameHash: false,
        EnableRangeTimings: false);

    public static NprDiagnosticsOptions Smoke { get; } = new(
        EnablePassTimings: true,
        EnableStepAllocationTracking: false,
        EnableDetailedStepNotes: false,
        EnableMemoryLogs: false,
        EnablePixelHash: false,
        EnableFrameHash: false,
        EnableRangeTimings: false);

    public static NprDiagnosticsOptions Benchmark { get; } = new(
        EnablePassTimings: true,
        EnableStepAllocationTracking: true,
        EnableDetailedStepNotes: true,
        EnableMemoryLogs: true,
        EnablePixelHash: false,
        EnableFrameHash: false,
        EnableRangeTimings: false);

    public static NprDiagnosticsOptions Parity { get; } = new(
        EnablePassTimings: true,
        EnableStepAllocationTracking: true,
        EnableDetailedStepNotes: true,
        EnableMemoryLogs: false,
        EnablePixelHash: true,
        EnableFrameHash: true,
        EnableRangeTimings: false);
}
