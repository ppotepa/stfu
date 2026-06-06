using STFU.Rendering.Abstractions.Execution;

namespace STFU.Rendering.Abstractions.Diagnostics;

public sealed record NprRenderParitySnapshot(
    long Revision,
    int Width,
    int Height,
    NprExecutionProfile ExecutionProfile,
    NprRenderContentKind ContentKind,
    string ActivePresetId,
    string ActivePipelineId,
    ulong StrokeFrameHash,
    ulong NprFrameHash,
    ulong DebugFrameHash,
    ulong PixelHash,
    int PathCount,
    int LayerCount,
    int ToneSurfaceCount);
