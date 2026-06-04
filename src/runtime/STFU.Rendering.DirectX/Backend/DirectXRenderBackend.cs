using System.Diagnostics;
using System.Runtime.Versioning;
using STFU.Logging;
using STFU.NPR.Debug;
using STFU.NPR.Graph;
using STFU.NPR.Rendering;
using STFU.Rendering.Abstractions.Backend;
using STFU.Rendering.Abstractions.Context;
using STFU.Rendering.Abstractions.Diagnostics;
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
    private static NprGraph? s_reusableGraph;

    public const string BackendId = "directx-d3d11";

    private readonly DirectXDevice _device;
    private readonly ICpuRenderBackend _cpuFallback;
    private readonly DxReadbackPass _readbackPass;
    private readonly DxNprFrameRenderer _frameRenderer;

    public DirectXRenderBackend(
        DirectXDevice device,
        ICpuRenderBackend cpuFallback,
        PixelSurfacePool surfacePool)
    {
        _device = device;
        _cpuFallback = cpuFallback;
        _readbackPass = new DxReadbackPass(device, surfacePool);
        _frameRenderer = new DxNprFrameRenderer(device);
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

    public ValueTask<NprRenderResult> RenderAsync(
        NprRenderRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request.ExecutionProfile == NprExecutionProfile.FullCpuReference)
        {
            StfuLog.Write(
                StfuLogDomain.RenderGpu,
                "fallback.cpu.profile",
                "FullCpuReference request routed to CPU fallback.",
                StfuLogLevel.Debug,
                new Dictionary<string, object?> { ["revision"] = request.Revision });
            return _cpuFallback.RenderAsync(request, cancellationToken);
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
            return _cpuFallback.RenderAsync(cpuFallbackRequest, cancellationToken);
        }

        var diagnostics = new NprRenderDiagnostics
        {
            Width = request.Width,
            Height = request.Height,
            WorkerCount = request.Budget.ResolveWorkerCount(),
            Notes = _device.Support.AdapterName
        };

        var total = Stopwatch.StartNew();
        var allocatedBefore = GC.GetTotalAllocatedBytes(false);
        StrokeFrame strokeFrame = StrokeFrame.Empty;
        NprFrame nprFrame = NprFrame.Empty;
        NprDebugFrame debugFrame = NprDebugFrame.Empty;

        if (request.ContentKind == NprRenderContentKind.NprPipeline)
        {
            if (request.Pipeline is null)
            {
                throw new InvalidOperationException("NprRenderRequest.Pipeline is required for GPU NPR rendering.");
            }

            var contextWatch = Stopwatch.StartNew();
            var context = NprRenderContextFactory.Create(request, s_reusableGraph ??= new NprGraph());
            contextWatch.Stop();
            diagnostics.AddTiming("BuildNprContext", contextWatch.Elapsed.TotalMilliseconds);

            var pipelineWatch = Stopwatch.StartNew();
            strokeFrame = request.Pipeline.Execute(context);
            pipelineWatch.Stop();
            diagnostics.AddTiming("NprPipeline.Execute", pipelineWatch.Elapsed.TotalMilliseconds, request.ActivePipelineId);

            foreach (var trace in context.StepTraces)
            {
                var notes = trace.AllocatedBytes > 0
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

        using var deviceLock = _device.Lock();
        var rentWatch = Stopwatch.StartNew();
        var gpuLease = _device.TexturePool.RentRenderTarget(request.Width, request.Height, GpuSurfaceFormat.Bgra8888Unorm);
        rentWatch.Stop();
        diagnostics.AddTiming("GpuTextureRent", rentWatch.Elapsed.TotalMilliseconds);

        try
        {
            if (!_device.Resources.TryGetTexture(gpuLease.Texture, out var target))
            {
                throw new InvalidOperationException("DirectX render target could not be resolved from resource registry.");
            }

            _frameRenderer.Render(target, request, strokeFrame, nprFrame, debugFrame, diagnostics, cancellationToken);

            if (request.Budget.RequireGpuReadback)
            {
                var readbackWatch = Stopwatch.StartNew();
                var pixelLease = _readbackPass.ReadToPixelSurface(gpuLease.Texture, cancellationToken);
                readbackWatch.Stop();
                diagnostics.AddTiming("GpuReadback", readbackWatch.Elapsed.TotalMilliseconds);
                gpuLease.Dispose();

                total.Stop();
                diagnostics.TotalMilliseconds = total.Elapsed.TotalMilliseconds;
                diagnostics.AllocatedBytes = Math.Max(0, GC.GetTotalAllocatedBytes(false) - allocatedBefore);

                return ValueTask.FromResult(new NprRenderResult
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
                });
            }

            total.Stop();
            diagnostics.TotalMilliseconds = total.Elapsed.TotalMilliseconds;
            diagnostics.AllocatedBytes = Math.Max(0, GC.GetTotalAllocatedBytes(false) - allocatedBefore);

            return ValueTask.FromResult(new NprRenderResult
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
            });
        }
        catch
        {
            gpuLease.Dispose();
            throw;
        }
    }
}
