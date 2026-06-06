using System.Diagnostics;
using STFU.Common.Math;
using STFU.NPR.Debug;
using STFU.NPR.Rendering;
using STFU.Rendering.Abstractions.Backend;
using STFU.Rendering.Abstractions.Context;
using STFU.Rendering.Abstractions.Diagnostics;
using STFU.Rendering.Abstractions.Execution;
using STFU.Rendering.Abstractions.Requests;
using STFU.Rendering.Abstractions.Surfaces;
using STFU.Rendering.Cpu.Rasterization;
using STFU.Strokes;

namespace STFU.Rendering.Cpu.Backend;

public sealed class FullCpuRenderBackend : ICpuRenderBackend
{
    [ThreadStatic]
    private static NprRenderContextScratch? s_reusableScratch;

    private readonly PixelSurfacePool _surfacePool;
    private readonly CpuNprFrameRasterizer _nprRasterizer = new();
    private readonly CpuMeshWireframeBuilder _meshBuilder = new();
    private readonly CpuRasterWorkspace _rasterWorkspace = new();

    public FullCpuRenderBackend(PixelSurfacePool surfacePool)
    {
        _surfacePool = surfacePool;
    }

    public NprBackendInfo Info { get; } = new(
        "full-cpu",
        "Full CPU Renderer",
        NprBackendKind.Cpu,
        NprBackendCapabilities.CpuSingleThread |
        NprBackendCapabilities.CpuParallel |
        NprBackendCapabilities.CpuTileRaster |
        NprBackendCapabilities.CpuToneRaster |
        NprBackendCapabilities.CpuStrokeRaster |
        NprBackendCapabilities.CpuMeshWireframe |
        NprBackendCapabilities.NprPipelineExecution |
        NprBackendCapabilities.PixelSurfaceOutput,
        "CPU-only NPR pipeline execution and pixel rasterization into BGRA8888 premultiplied PixelSurface.");

    public ValueTask<NprRenderResult> RenderAsync(
        NprRenderRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var resolvedWorkerCount = request.Budget.ResolveWorkerCount();

        if (request.ExecutionProfile != NprExecutionProfile.FullCpuReference)
        {
            throw new NotSupportedException($"FullCpuRenderBackend supports only {nameof(NprExecutionProfile.FullCpuReference)}.");
        }

        var diagnostics = new NprRenderDiagnostics
        {
            Width = request.Width,
            Height = request.Height,
            WorkerCount = resolvedWorkerCount,
            WorkerBudgetMode = request.Budget.WorkerBudgetMode,
            ProcessorCount = Environment.ProcessorCount
        };

        var total = Stopwatch.StartNew();
        var allocatedBefore = GC.GetTotalAllocatedBytes(false);
        StrokeFrame strokeFrame = StrokeFrame.Empty;
        NprFrame nprFrame = NprFrame.Empty;
        NprDebugFrame debugFrame = NprDebugFrame.Empty;
        List<CpuStrokeSegment>? meshSegments = null;

        if (request.ContentKind == NprRenderContentKind.MeshWireframe)
        {
            var meshWatch = Stopwatch.StartNew();
            meshSegments = _meshBuilder.BuildSegments(
                request.Scene,
                request.Assets,
                request.Analysis,
                request.Camera,
                request.Width,
                request.Height,
                request.Settings,
                request.Theme,
                request.Quality.MeshWireframeTopologyMode);
            meshWatch.Stop();
            diagnostics.PathCount = meshSegments.Count;
            diagnostics.AddTiming(
                "CpuMeshWireframe",
                meshWatch.Elapsed.TotalMilliseconds,
                $"topology={request.Quality.MeshWireframeTopologyMode}; edges={_meshBuilder.LastTopologyEdgeCount}; segments={meshSegments.Count}");
        }
        else
        {
            if (request.Pipeline is null)
            {
                throw new InvalidOperationException("NprRenderRequest.Pipeline is required for NprRenderContentKind.NprPipeline.");
            }

            var contextWatch = Stopwatch.StartNew();
            var context = NprRenderContextFactory.CreateWithScratch(
                request,
                s_reusableScratch ??= new NprRenderContextScratch(),
                cancellationToken);
            contextWatch.Stop();
            diagnostics.AddTiming("BuildNprContext", contextWatch.Elapsed.TotalMilliseconds);

            cancellationToken.ThrowIfCancellationRequested();

            var pipelineWatch = Stopwatch.StartNew();
            strokeFrame = request.Pipeline.Execute(context);
            pipelineWatch.Stop();
            diagnostics.AddTiming("NprPipeline.Execute", pipelineWatch.Elapsed.TotalMilliseconds, request.ActivePipelineId);
            foreach (var trace in context.StepTraces)
            {
                var notes = request.DiagnosticsOptions?.EnableStepAllocationTracking == true && trace.AllocatedBytes > 0
                    ? $"{trace.Notes}; alloc={trace.AllocatedBytes}"
                    : trace.Notes;
                diagnostics.AddTiming($"NprStep.{trace.StepName}", trace.Milliseconds, notes);
            }

            nprFrame = context.NprFrame;
            debugFrame = context.DebugFrame;
            diagnostics.PathCount = strokeFrame.Paths.Count;
            diagnostics.LayerCount = nprFrame.Layers.Count;
            diagnostics.ToneSurfaceCount = context.Graph.ToneSurfaces.Count;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var lease = _surfacePool.Rent(request.Width, request.Height, PixelSurfaceFormat.Bgra8888Premultiplied);
        try
        {
            var rasterWatch = Stopwatch.StartNew();
            if (meshSegments is not null)
            {
                _nprRasterizer.RasterizeMeshWireframe(lease.Surface, request, meshSegments, _rasterWorkspace, cancellationToken);
            }
            else
            {
                _nprRasterizer.Rasterize(lease.Surface, request, strokeFrame, nprFrame, _rasterWorkspace, cancellationToken);
            }
            rasterWatch.Stop();
            diagnostics.AddTiming("CpuRasterize", rasterWatch.Elapsed.TotalMilliseconds);

            total.Stop();
            diagnostics.TotalMilliseconds = total.Elapsed.TotalMilliseconds;
            diagnostics.AllocatedBytes = NumericMath.AtLeast(GC.GetTotalAllocatedBytes(false) - allocatedBefore, 0);

            return ValueTask.FromResult(new NprRenderResult
            {
                Revision = request.Revision,
                Status = NprRenderStatus.Completed,
                ExecutionProfile = request.ExecutionProfile,
                OutputKind = NprRenderOutputKind.PixelSurface,
                PixelSurfaceLease = lease,
                StrokeFrame = strokeFrame,
                NprFrame = nprFrame,
                DebugFrame = debugFrame,
                Diagnostics = diagnostics
            });
        }
        catch
        {
            lease.Dispose();
            throw;
        }
    }
}
