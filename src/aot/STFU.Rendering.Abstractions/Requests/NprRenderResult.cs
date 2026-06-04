using STFU.NPR.Debug;
using STFU.NPR.Rendering;
using STFU.Rendering.Abstractions.Diagnostics;
using STFU.Rendering.Abstractions.Execution;
using STFU.Rendering.Abstractions.Gpu;
using STFU.Rendering.Abstractions.Surfaces;
using STFU.Strokes;

namespace STFU.Rendering.Abstractions.Requests;

public sealed class NprRenderResult : IDisposable
{
    public required long Revision { get; init; }

    public required NprRenderStatus Status { get; init; }

    public required NprExecutionProfile ExecutionProfile { get; init; }

    public required NprRenderOutputKind OutputKind { get; init; }

    public PixelSurfaceLease? PixelSurfaceLease { get; init; }

    public GpuTextureLease? GpuTextureLease { get; init; }

    public StrokeFrame StrokeFrame { get; init; } = StrokeFrame.Empty;

    public NprFrame NprFrame { get; init; } = NprFrame.Empty;

    public NprDebugFrame DebugFrame { get; init; } = NprDebugFrame.Empty;

    public Exception? Exception { get; init; }

    public required NprRenderDiagnostics Diagnostics { get; init; }

    public void Dispose()
    {
        PixelSurfaceLease?.Dispose();
        GpuTextureLease?.Dispose();
    }
}
