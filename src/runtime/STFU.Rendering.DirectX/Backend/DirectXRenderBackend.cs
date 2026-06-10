using System.Diagnostics;
using System.Runtime.Versioning;
using STFU.Common.Math;
using STFU.Logging;
using STFU.NPR.Debug;
using STFU.NPR.Graph;
using STFU.NPR.Pipeline;
using STFU.Rendering.Abstractions.Diagnostics;
using STFU.NPR.Rendering;
using STFU.Rendering.Abstractions.Backend;
using STFU.Rendering.Abstractions.Context;
using STFU.Rendering.Abstractions.Execution;
using STFU.Rendering.Abstractions.Gpu;
using STFU.Rendering.Abstractions.Requests;
using STFU.Rendering.Abstractions.Surfaces;
using STFU.Rendering.DirectX.Device;
using STFU.Rendering.DirectX.Passes;
using STFU.Strokes;

namespace STFU.Rendering.DirectX.Backend;

[SupportedOSPlatform("windows")]
public sealed class DirectXRenderBackend : IGpuRenderBackend
{
    [ThreadStatic]
    private static NprRenderContextScratch? s_reusableScratch;

    public const string BackendId = "directx-d3d11";

    private readonly DirectXDevice _device;
    private readonly ICpuRenderBackend _cpuFallback;
    private readonly DxReadbackPass _readbackPass;
    private readonly DxNprFrameRenderer _frameRenderer;
    private readonly DxGpuVisibilityBufferPass _visibilityPass;
    private long _lastMemoryLogRevision;

    public DirectXRenderBackend(
        DirectXDevice device,
        ICpuRenderBackend cpuFallback,
        PixelSurfacePool surfacePool)
    {
        _device = device;
        _cpuFallback = cpuFallback;
        _readbackPass = new DxReadbackPass(device, surfacePool);
        _frameRenderer = new DxNprFrameRenderer(device);
        _visibilityPass = new DxGpuVisibilityBufferPass(device);
    }

    public bool IsAvailable => OperatingSystem.IsWindows() && !_device.IsDisposed;

    public NprBackendInfo Info { get; } = new(
        BackendId,
        "DirectX D3D11 GPU Renderer",
        NprBackendKind.Gpu,
        NprBackendCapabilities.GpuGraphics |
        NprBackendCapabilities.GpuPresentation |
        NprBackendCapabilities.GpuReadback |
        NprBackendCapabilities.GpuRenderTargets |
        NprBackendCapabilities.GpuTextureUpload |
        NprBackendCapabilities.GpuStrokeRaster |
        NprBackendCapabilities.GpuToneRaster |
        NprBackendCapabilities.GpuMeshWireframe |
        NprBackendCapabilities.GpuDebugOverlay |
        NprBackendCapabilities.GpuFinalComposite |
        NprBackendCapabilities.NprPipelineExecution |
        NprBackendCapabilities.GpuTextureOutput |
        NprBackendCapabilities.PixelSurfaceOutput,
        "CPU-driven NPR pipeline with Direct3D11 GPU rasterization and fallback-to-CPU integration scaffold.");

    public async ValueTask<NprRenderResult> RenderAsync(
        NprRenderRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var resolvedWorkerCount = request.Budget.ResolveWorkerCount();

        if (request.ExecutionProfile == NprExecutionProfile.FullCpuReference)
        {
            StfuLog.Write(
                StfuLogDomain.RenderGpu,
                "fallback.cpu.profile",
                "FullCpuReference request routed to CPU fallback.",
                StfuLogLevel.Debug,
                new Dictionary<string, object?> { ["revision"] = request.Revision });
            return await _cpuFallback.RenderAsync(request, cancellationToken);
        }

        if (request.ExecutionProfile != NprExecutionProfile.CpuDrivenGpuAccelerated)
        {
            throw new NotSupportedException($"DirectXRenderBackend does not support {request.ExecutionProfile}.");
        }

        if (!IsAvailable)
        {
            StfuLog.Write(
                StfuLogDomain.RenderGpu,
                "fallback.cpu.unavailable",
                "DirectX backend unavailable; rendering with Full CPU.",
                StfuLogLevel.Warning,
                new Dictionary<string, object?> { ["revision"] = request.Revision });
            var cpuFallbackRequest = request with
            {
                ExecutionProfile = NprExecutionProfile.FullCpuReference
            };
            return await _cpuFallback.RenderAsync(cpuFallbackRequest, cancellationToken);
        }

        var diagnostics = new NprRenderDiagnostics
        {
            Width = request.Width,
            Height = request.Height,
            WorkerCount = resolvedWorkerCount,
            WorkerBudgetMode = request.Budget.WorkerBudgetMode,
            ProcessorCount = Environment.ProcessorCount,
            Notes = _device.Support.AdapterName
        };

        var total = Stopwatch.StartNew();
        var allocatedBefore = GC.GetTotalAllocatedBytes(false);
        StrokeFrame strokeFrame = StrokeFrame.Empty;
        NprFrame nprFrame = NprFrame.Empty;
        NprDebugFrame debugFrame = NprDebugFrame.Empty;
        NprGraph? pipelineGraph = null;

        if (request.ContentKind == NprRenderContentKind.NprPipeline)
        {
            if (request.Pipeline is null)
            {
                throw new InvalidOperationException("NprRenderRequest.Pipeline is required for GPU NPR rendering.");
            }

            var contextWatch = Stopwatch.StartNew();
            var context = NprRenderContextFactory.CreateWithScratch(
                request,
                s_reusableScratch ??= new NprRenderContextScratch(),
                cancellationToken);
            contextWatch.Stop();
            diagnostics.AddTiming("BuildNprContext", contextWatch.Elapsed.TotalMilliseconds);

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
            pipelineGraph = context.Graph;
            diagnostics.PathCount = strokeFrame.Paths.Count;
            diagnostics.LayerCount = nprFrame.Layers.Count;
            diagnostics.ToneSurfaceCount = context.Graph.ToneSurfaces.Count;
            CopyPipelineCounters(context, diagnostics);
        }

        var rentWatch = Stopwatch.StartNew();
        GpuTextureLease gpuLease;
        using (_device.Lock())
        {
            gpuLease = _device.TexturePool.RentRenderTarget(request.Width, request.Height, GpuSurfaceFormat.Bgra8888Unorm);
        }

        rentWatch.Stop();
        diagnostics.AddTiming("GpuTextureRent", rentWatch.Elapsed.TotalMilliseconds);

        try
        {
            DirectXTextureResource target;
            using (_device.Lock())
            {
                if (!_device.Resources.TryGetTexture(gpuLease.Texture, out target!))
                {
                    throw new InvalidOperationException("DirectX render target could not be resolved from resource registry.");
                }
            }

            if (target is null)
            {
                throw new InvalidOperationException("DirectX render target could not be resolved from resource registry.");
            }

            if (request.Quality.UseGpuVisibilityBuffer &&
                request.ContentKind == NprRenderContentKind.NprPipeline &&
                pipelineGraph is not null)
            {
                var visibilityStats = _visibilityPass.Execute(
                    pipelineGraph,
                    request.Width,
                    request.Height,
                    diagnostics,
                    cancellationToken);

                diagnostics.VisibilityParity = visibilityStats;
                if (visibilityStats.ShouldFallback(request.Budget.GpuVisibilityRequiredMatchRatio))
                {
                    if (!request.Budget.AllowGpuVisibilityFallback)
                    {
                        throw new InvalidOperationException(
                            $"GPU visibility mismatch ratio is {visibilityStats.MatchRatio:0.000} with {visibilityStats.MismatchCount} mismatches, and fallback is disabled.");
                    }

                    var fallbackReason = VisibilityParityStats.FallbackReasonMismatch;
                    var fallbackReadback = await RenderWithCpuFallbackAsync(
                        request,
                        NprRenderOutputKind.PixelSurface,
                        visibilityStats,
                        fallbackReason,
                        cancellationToken);
                    return await ApplyFallbackTelemetryAsync(fallbackReadback, diagnostics, visibilityStats, fallbackReason);
                }
            }

            _frameRenderer.Render(target, request, strokeFrame, nprFrame, debugFrame, diagnostics, cancellationToken);

            if (request.Budget.RequireGpuReadback)
            {
                var priorReadbacks = _readbackPass.Counters.Readbacks;
                if (!request.Budget.AllowGpuReadback)
                {
                    throw new InvalidOperationException("GPU readback is required for this request, but GPU readback is disabled by the frame budget.");
                }

                var readbackWatch = Stopwatch.StartNew();
                var pixelLease = _readbackPass.ReadToPixelSurface(gpuLease.Texture, cancellationToken);
                readbackWatch.Stop();
                var readbacks = _readbackPass.Counters.Readbacks - priorReadbacks;
                var readbackBytes = pixelLease.Surface.Stride * pixelLease.Surface.Height;
                diagnostics.Readbacks += readbacks;
                diagnostics.AddTiming(
                    "GpuReadback",
                    readbackWatch.Elapsed.TotalMilliseconds,
                    $"readbacks={readbacks}, bytes={readbackBytes}");
                gpuLease.Dispose();

                total.Stop();
                diagnostics.TotalMilliseconds = total.Elapsed.TotalMilliseconds;
                diagnostics.AllocatedBytes = NumericMath.AtLeast(GC.GetTotalAllocatedBytes(false) - allocatedBefore, 0);
                LogMemoryIfNeeded(request, diagnostics);

            return new NprRenderResult
            {
                Revision = request.Revision,
                Status = NprRenderStatus.Completed,
                ExecutionProfile = request.ExecutionProfile,
                OutputKind = NprRenderOutputKind.PixelSurface,
                PixelSurfaceLease = pixelLease,
                StrokeFrame = strokeFrame,
                NprFrame = nprFrame,
                DebugFrame = debugFrame,
                Diagnostics = diagnostics
            };
            }

            if (!request.Budget.PreferGpuPresentation && !request.Budget.AllowGpuReadback)
            {
                throw new InvalidOperationException("GPU readback is disabled for this request, but direct GPU presentation was not selected.");
            }

            total.Stop();
            diagnostics.TotalMilliseconds = total.Elapsed.TotalMilliseconds;
            diagnostics.AllocatedBytes = NumericMath.AtLeast(GC.GetTotalAllocatedBytes(false) - allocatedBefore, 0);
            LogMemoryIfNeeded(request, diagnostics);

            return new NprRenderResult
            {
                Revision = request.Revision,
                Status = NprRenderStatus.Completed,
                ExecutionProfile = request.ExecutionProfile,
                OutputKind = NprRenderOutputKind.GpuTexture,
                GpuTextureLease = gpuLease,
                StrokeFrame = strokeFrame,
                NprFrame = nprFrame,
                DebugFrame = debugFrame,
                Diagnostics = diagnostics
            };
        }
        catch
        {
            gpuLease.Dispose();
            throw;
        }
    }

    private async ValueTask<NprRenderResult> RenderWithCpuFallbackAsync(
        NprRenderRequest request,
        NprRenderOutputKind requestedOutput,
        VisibilityParityStats visibilityStats,
        string fallbackReason,
        CancellationToken cancellationToken)
    {
        var cpuFallbackRequest = request with
        {
            ExecutionProfile = NprExecutionProfile.FullCpuReference,
            Quality = request.Quality with
            {
                UseGpuVisibilityBuffer = false
            },
            Budget = request.Budget with
            {
                RequireGpuReadback = requestedOutput == NprRenderOutputKind.PixelSurface || request.Budget.RequireGpuReadback,
                AllowGpuReadback = request.Budget.AllowGpuReadback,
                PreferGpuPresentation = requestedOutput == NprRenderOutputKind.GpuTexture
            }
        };

        StfuLog.Write(
            StfuLogDomain.RenderGpu,
            "visibility.fallback",
            $"revision={request.Revision}, fallbackReason={fallbackReason}, requestedOutput={requestedOutput}");

        var cpuResult = await _cpuFallback.RenderAsync(cpuFallbackRequest, cancellationToken);
        return await ApplyFallbackTelemetryAsync(
            cpuResult,
            diagnostics: null,
            visibilityStats,
            fallbackReason);
    }

    private static async ValueTask<NprRenderResult> ApplyFallbackTelemetryAsync(
        NprRenderResult result,
        NprRenderDiagnostics? diagnostics,
        VisibilityParityStats visibilityStats,
        string fallbackReason)
    {
        var sourceDiagnostics = diagnostics ?? result.Diagnostics;
        sourceDiagnostics.VisibilityParity = new VisibilityParityStats
        {
            CpuVisibleFaces = visibilityStats.CpuVisibleFaces,
            GpuVisibleFaces = visibilityStats.GpuVisibleFaces,
            MatchingFaces = visibilityStats.MatchingFaces,
            CpuOnlyFaces = visibilityStats.CpuOnlyFaces,
            GpuOnlyFaces = visibilityStats.GpuOnlyFaces,
            FallbackUsed = true,
            FallbackReason = fallbackReason,
            MatchRatio = visibilityStats.MatchRatio,
            Passed = false
        };
        sourceDiagnostics.Notes = string.IsNullOrWhiteSpace(sourceDiagnostics.Notes)
            ? $"visibilityFallback={fallbackReason}"
            : $"{sourceDiagnostics.Notes}; visibilityFallback={fallbackReason}";

        await ValueTask.CompletedTask;
        return result;
    }

    private static void CopyPipelineCounters(NprContext context, NprRenderDiagnostics diagnostics)
    {
        foreach (var pair in context.Counters.Values)
        {
            diagnostics.Counters[pair.Key] = pair.Value;
        }
    }

    private void LogMemoryIfNeeded(NprRenderRequest request, NprRenderDiagnostics diagnostics)
    {
        if (request.DiagnosticsOptions?.EnableMemoryLogs != true)
        {
            return;
        }

        if (request.Revision != 1 && request.Revision - _lastMemoryLogRevision < 120)
        {
            return;
        }

        _lastMemoryLogRevision = request.Revision;
        using var process = Process.GetCurrentProcess();
        var gcInfo = GC.GetGCMemoryInfo();
        var renderTargets = _device.TexturePool.Snapshot();
        var readbacks = _device.ReadbackTexturePool.Snapshot();

        StfuLog.Write(
            StfuLogDomain.Memory,
            "directx.render",
            $"revision={request.Revision}",
            properties: new Dictionary<string, object?>
            {
                ["revision"] = request.Revision,
                ["profile"] = request.ExecutionProfile,
                ["output"] = request.Budget.RequireGpuReadback ? "readback" : "texture",
                ["workingSetMb"] = BufferSizingMath.ToMegabytes(process.WorkingSet64),
                ["privateMb"] = BufferSizingMath.ToMegabytes(process.PrivateMemorySize64),
                ["gcHeapMb"] = BufferSizingMath.ToMegabytes(GC.GetTotalMemory(false)),
                ["gcHeapSizeMb"] = BufferSizingMath.ToMegabytes(gcInfo.HeapSizeBytes),
                ["allocatedFrameMb"] = BufferSizingMath.ToMegabytes(diagnostics.AllocatedBytes),
                ["rtRetained"] = renderTargets.RetainedCount,
                ["rtCreated"] = renderTargets.CreatedCount,
                ["rtReused"] = renderTargets.ReusedCount,
                ["rtDisposed"] = renderTargets.DisposedCount,
                ["readbackRetained"] = readbacks.RetainedCount,
                ["readbackCreated"] = readbacks.CreatedCount,
                ["readbackReused"] = readbacks.ReusedCount,
                ["readbackDisposed"] = readbacks.DisposedCount
            });
    }

}
