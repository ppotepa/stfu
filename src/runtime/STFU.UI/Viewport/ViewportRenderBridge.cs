using System.Diagnostics;
using Avalonia.Threading;
using STFU.Common.Math;
using STFU.Messaging.Commands;
using STFU.NPR.Composition;
using STFU.NPR.Debug;
using STFU.Logging;
using STFU.Parallelism;
using STFU.Rendering.Abstractions.Execution;
using STFU.Rendering.Abstractions.Requests;
using STFU.Strokes;
using STFU.UI.Bridge.Renderer;
using STFU.UI.Bridge.Session;
using STFU.UI.Styling;
using STFU.Viewport;
using STFU.Viewport.Commands;
using System.Linq;

namespace STFU.UI;

internal sealed class ViewportRenderBridge : IDisposable
{
    // Direct/readback budget contract is built through ViewportRenderRequestFactory:
    // PreferGpuPresentation, AllowGpuReadback, RequireGpuReadback.
    private readonly ViewportFrameCoordinator _coordinator;

    public ViewportRenderBridge(
        UiEngineSession session,
        AvaloniaBitmapPresenter presenter,
        Action requestPresent,
        Action requestNextFrame,
        DirectXViewportPresenter? directXPresenter = null)
    {
        _coordinator = new ViewportFrameCoordinator(
            session,
            presenter,
            requestPresent,
            requestNextFrame,
            directXPresenter);
    }

    public bool IsDirectGpuPresenting => _coordinator.IsDirectGpuPresenting;

    public bool IsDirectPresentationSuppressed => _coordinator.IsDirectPresentationSuppressed;

    public void RequestFrame(int width, int height, ViewportRenderMode viewportRenderMode)
    {
        _coordinator.Tick(width, height, viewportRenderMode);
    }

    public bool ApplyPendingResultIfAny()
    {
        return _coordinator.ApplyPendingResultIfAny();
    }

    public void ResetDirectPresentationFallback()
    {
        _coordinator.ResetDirectPresentationFallback();
    }

    public void Dispose()
    {
        _coordinator.Dispose();
    }
}

internal sealed class ViewportFrameCoordinator : IDisposable
{
    private readonly NprRenderOptimizerMode _optimizerMode;
    private readonly UiEngineSession _session;
    private readonly INprRenderScheduler _scheduler;
    private readonly AvaloniaBitmapPresenter _bitmapPresenter;
    private readonly DirectXViewportPresenter? _directXPresenter;
    private readonly Action _requestPresent;
    private readonly Action _requestNextFrame;
    private readonly object _gate = new();
    private readonly ViewportFrameState _state = new();
    private readonly RendererRuntimePlanResolver _runtimePlanResolver = new();
    private readonly ViewportRenderRequestFactory _requestFactory;
    private NprRenderResult? _pendingResult;
    private DateTimeOffset _lastDrawTick = DateTimeOffset.Now;
    private bool _disposed;
    private const int DirectPresentFallbackThreshold = 3;

    public ViewportFrameCoordinator(
        UiEngineSession session,
        AvaloniaBitmapPresenter presenter,
        Action requestPresent,
        Action requestNextFrame,
        DirectXViewportPresenter? directXPresenter = null)
    {
        _session = session;
        _optimizerMode = NprRenderOptimizerModeResolver.ResolveFromEnvironment();
        _requestFactory = new ViewportRenderRequestFactory(session, _optimizerMode);
        _scheduler = session.RenderScheduler;
        _bitmapPresenter = presenter;
        _directXPresenter = directXPresenter;
        _requestPresent = requestPresent;
        _requestNextFrame = requestNextFrame;
        _scheduler.RenderCompleted += OnRenderCompleted;
    }

    public bool IsDirectGpuPresenting =>
        _state.IsDirectGpuPresenting(_directXPresenter, DirectPresentFallbackThreshold);

    public bool IsDirectPresentationSuppressed =>
        _state.ConsecutiveDirectPresentSkips >= DirectPresentFallbackThreshold;

    public void ResetDirectPresentationFallback()
    {
        _state.ResetDirectPresentFailures();
    }

    public void Tick(int width, int height, ViewportRenderMode viewportRenderMode)
    {
        if (_disposed)
        {
            return;
        }

        width = NumericMath.AtLeast(width, 1);
        height = NumericMath.AtLeast(height, 1);

        _state.UpdateViewportSize(width, height);

        lock (_gate)
        {
            if (_state.RenderInFlight)
            {
                if (_state.RecordDeferredFrame())
                {
                    StfuLog.Write(
                        StfuLogDomain.Viewport,
                        "frame.request_deferred",
                        $"latestEnqueued={_state.LastEnqueuedRevision}",
                        StfuLogLevel.Debug,
                        new Dictionary<string, object?>
                        {
                            ["latestEnqueued"] = _state.LastEnqueuedRevision,
                            ["latestCompleted"] = _state.LastCompletedRevision,
                            ["schedulerLatestRequested"] = _scheduler.LatestRequestedRevision,
                            ["schedulerLatestCompleted"] = _scheduler.LatestCompletedRevision,
                            ["width"] = width,
                            ["height"] = height
                        });
                }

                return;
            }
        }

        UpdateDefaultDrawProgress();

        if (_session.Workspace.Assets.TickAnimation())
        {
            _session.FrameHistory.Reset();
        }

        var revision = Interlocked.Increment(ref _state.NextRevision);
        var renderer = _session.Workspace.Renderer;
        var runtimePlan = _runtimePlanResolver.Resolve(
            renderer,
            _session.HasGpuRenderer,
            _directXPresenter?.IsAttached == true,
            IsDirectPresentationSuppressed,
            IsDirectGpuPresenting,
            _session.GpuRenderBackend?.Info.Name);
        var requestBuild = _requestFactory.Create(
            revision,
            width,
            height,
            viewportRenderMode,
            runtimePlan);
        renderer.UpdateRuntimeStatus(
            requestBuild.RuntimeStatus.EffectiveBackend,
            requestBuild.RuntimeStatus.EffectiveApi,
            requestBuild.RuntimeStatus.EffectivePresentation,
            requestBuild.RuntimeStatus.SurfaceMode,
            requestBuild.RuntimeStatus.DirectPresenterAvailable,
            requestBuild.RuntimeStatus.DirectSuppressed,
            requestBuild.RuntimeStatus.PreferGpuPresentation,
            requestBuild.RuntimeStatus.RequireGpuReadback,
            requestBuild.RuntimeStatus.AllowGpuReadback,
            requestBuild.RuntimeStatus.ShowDirectHost,
            requestBuild.RuntimeStatus.DrawBitmap,
            requestBuild.RuntimeStatus.AdapterName,
            requestBuild.RuntimeStatus.StatusMessage);
        if (revision == 1 || revision % 120 == 0)
        {
            bool renderInFlight;
            lock (_gate)
            {
                renderInFlight = _state.RenderInFlight;
            }

            StfuLog.Write(
                StfuLogDomain.Viewport,
                "frame.request",
                viewportRenderMode.ToString(),
                properties: new Dictionary<string, object?>
                {
                    ["revision"] = revision,
                    ["width"] = width,
                    ["height"] = height,
                    ["profile"] = requestBuild.RuntimePlan.EffectiveProfile,
                    ["presentation"] = requestBuild.RuntimeStatus.EffectivePresentation,
                    ["surfaceMode"] = requestBuild.RuntimePlan.SurfaceMode,
                    ["workerBudgetMode"] = requestBuild.FrameBudget.WorkerBudgetMode,
                    ["maxRenderWorkers"] = requestBuild.FrameBudget.MaxWorkerThreads,
                    ["resolvedWorkers"] = requestBuild.ResolvedWorkerCount,
                    ["processorCount"] = WorkerBudget.LogicalProcessorCount,
                    ["tileSize"] = requestBuild.FrameBudget.TileSize,
                    ["tileParallelism"] = requestBuild.FrameBudget.EnableTileParallelism,
                    ["directGpu"] = requestBuild.UseDirectGpuPresenter,
                    ["requireGpuReadback"] = requestBuild.FrameBudget.RequireGpuReadback,
                    ["allowGpuReadback"] = requestBuild.FrameBudget.AllowGpuReadback,
                    ["renderInFlight"] = renderInFlight,
                    ["cameraPosition"] = _session.CameraRig.Camera.Position.ToString(),
                    ["cameraTarget"] = _session.CameraRig.Camera.Target.ToString(),
                    ["cameraFov"] = _session.CameraRig.Camera.FieldOfViewDegrees,
                    ["latestRequested"] = _scheduler.LatestRequestedRevision,
                    ["latestCompleted"] = _scheduler.LatestCompletedRevision,
                    ["presentSkips"] = _state.ConsecutiveDirectPresentSkips
                });
        }

        lock (_gate)
        {
            _state.RenderInFlight = true;
            _state.LastEnqueuedRevision = revision;
            _state.DeferredFrameRequested = false;
            _state.RememberRuntimeStatus(revision, requestBuild.RuntimeStatus);
        }

        _ = _scheduler.EnqueueAsync(requestBuild.Request);
    }

    public bool ApplyPendingResultIfAny()
    {
        NprRenderResult? result;
        lock (_gate)
        {
            result = _pendingResult;
            _pendingResult = null;
        }

        if (result is null)
        {
            return false;
        }

        using (result)
        {
            ViewportRuntimeStatus requestStatus;
            lock (_gate)
            {
                requestStatus = _state.ConsumeRuntimeStatus(result.Revision) ?? new ViewportRuntimeStatus(
                    EffectiveBackend: _session.Workspace.Renderer.EffectiveBackend,
                    EffectiveApi: _session.Workspace.Renderer.EffectiveApi,
                    EffectivePresentation: _session.Workspace.Renderer.EffectivePresentation,
                    SurfaceMode: _session.Workspace.Renderer.SurfaceMode,
                    DirectPresenterAvailable: _session.Workspace.Renderer.DirectPresenterAvailable,
                    DirectSuppressed: _session.Workspace.Renderer.DirectSuppressed,
                    PreferGpuPresentation: _session.Workspace.Renderer.PreferGpuPresentation,
                    RequireGpuReadback: _session.Workspace.Renderer.RequireGpuReadback,
                    AllowGpuReadback: _session.Workspace.Renderer.AllowGpuReadback,
                    ShowDirectHost: _session.Workspace.Renderer.ShowDirectHost,
                    DrawBitmap: _session.Workspace.Renderer.DrawBitmap,
                    AdapterName: _session.Workspace.Renderer.AdapterName,
                    StatusMessage: _session.Workspace.Renderer.StatusMessage,
                    LastOutputKind: _session.Workspace.Renderer.LastOutputKind,
                    GpuReadbackMs: _session.Workspace.Renderer.GpuReadbackMs);
            }

            if (result.Status is NprRenderStatus.Cancelled or NprRenderStatus.Dropped)
            {
                return false;
            }

            if (result.Status != NprRenderStatus.Completed)
            {
                if (result.Exception is not null)
                {
                    StfuUiLog.Write($"Viewport render failed: {result.Exception.Message}");
                }

                StfuLog.Write(
                    StfuLogDomain.Errors,
                    "render.failed",
                    result.Exception?.Message ?? result.Status.ToString(),
                    StfuLogLevel.Error,
                    new Dictionary<string, object?>
                    {
                        ["revision"] = result.Revision,
                        ["status"] = result.Status,
                        ["profile"] = result.ExecutionProfile
                    },
                    result.Exception);

                _state.LastPresentedWithGpuTexture = false;
                return false;
            }

            if (result.OutputKind == NprRenderOutputKind.GpuTexture)
            {
                var displayedPresentation = false;
                var presenter = _directXPresenter;
                var presenterAttached = presenter?.IsAttached ?? false;
                var hasLease = result.GpuTextureLease is not null;
                var availability = presenter is null
                    ? DirectXPresentAvailability.NotAttached
                    : presenter.GetAvailability(result);

                var presented = false;
                if (presenter is null)
                {
                    StfuUiLog.Write("GPU texture result arrived without an attached DirectX presenter; frame skipped.");
                }
                else if (availability == DirectXPresentAvailability.Ready)
                {
                    presented = presenter.TryPresent(result, out availability);
                    if (presented)
                    {
                        StfuLog.Write(
                            StfuLogDomain.Viewport,
                            "gpu_present.success",
                            $"revision={result.Revision}",
                            StfuLogLevel.Debug,
                            new Dictionary<string, object?>
                            {
                                ["revision"] = result.Revision,
                                ["outputKind"] = result.OutputKind,
                                ["hasGpuLease"] = hasLease,
                                ["isAttached"] = presenterAttached,
                                ["availability"] = availability,
                                ["status"] = result.Status,
                                ["sourceWidth"] = result.GpuTextureLease?.Texture.Width,
                                ["sourceHeight"] = result.GpuTextureLease?.Texture.Height,
                                ["swapchainWidth"] = presenter.SwapChainWidth,
                                ["swapchainHeight"] = presenter.SwapChainHeight
                            });
                    }
                    else
                    {
                        StfuLog.Write(
                            StfuLogDomain.Viewport,
                            "gpu_present.failed",
                            $"revision={result.Revision} reason={availability}",
                            StfuLogLevel.Warning,
                            new Dictionary<string, object?>
                            {
                                ["revision"] = result.Revision,
                                ["outputKind"] = result.OutputKind,
                                ["hasGpuLease"] = hasLease,
                                ["isAttached"] = presenterAttached,
                                ["availability"] = availability,
                                ["status"] = result.Status,
                                ["sourceWidth"] = result.GpuTextureLease?.Texture.Width,
                                ["sourceHeight"] = result.GpuTextureLease?.Texture.Height,
                                ["swapchainWidth"] = presenter.SwapChainWidth,
                                ["swapchainHeight"] = presenter.SwapChainHeight
                            });
                    }
                }
                else
                {
                    StfuLog.Write(
                        StfuLogDomain.Viewport,
                        "gpu_present.skipped",
                        $"revision={result.Revision} reason={availability}",
                        StfuLogLevel.Warning,
                        new Dictionary<string, object?>
                        {
                            ["revision"] = result.Revision,
                            ["outputKind"] = result.OutputKind,
                            ["hasGpuLease"] = hasLease,
                            ["isAttached"] = presenterAttached,
                            ["availability"] = availability,
                            ["status"] = result.Status
                        });
                }

                if (presented)
                {
                    displayedPresentation = true;
                    _state.ResetDirectPresentFailures();
                    _state.LastPresentedWithGpuTexture = true;
                }
                else
                {
                    _state.ConsecutiveDirectPresentSkips++;
                    if (_state.ConsecutiveDirectPresentSkips >= DirectPresentFallbackThreshold &&
                        !_state.DirectPresentFallbackNotified)
                    {
                        StfuLog.Write(
                            StfuLogDomain.Viewport,
                            "gpu_present.fallback_suppressed",
                            $"revision={result.Revision} consecutiveSkips={_state.ConsecutiveDirectPresentSkips}",
                            StfuLogLevel.Warning,
                            new Dictionary<string, object?>
                            {
                                ["revision"] = result.Revision,
                                ["consecutiveSkips"] = _state.ConsecutiveDirectPresentSkips,
                                ["reason"] = availability
                            });

                        _state.DirectPresentFallbackNotified = true;
                        _state.LastPresentedWithGpuTexture = false;
                    }

                    if (presenter is null)
                    {
                        _state.LastPresentedWithGpuTexture = false;
                    }
                    else if (_state.LastPresentedWithGpuTexture)
                    {
                        // Keep direct-present status if this is a temporary skip after an active session;
                        // the next successful frame will clear any stale state.
                    }
                }
                UpdateRuntimeStatusFromResult(
                    result,
                    displayedPresentation,
                    requestStatus);
            }
            else
            {
                var displayedPresentation = false;
                if (!_bitmapPresenter.TryPresent(result, out var bitmapAvailability))
                {
                    StfuLog.Write(
                        StfuLogDomain.Viewport,
                        "bitmap_present.skipped",
                        $"revision={result.Revision} reason={bitmapAvailability}",
                        StfuLogLevel.Warning,
                        new Dictionary<string, object?>
                        {
                            ["revision"] = result.Revision,
                            ["outputKind"] = result.OutputKind,
                            ["availability"] = bitmapAvailability,
                            ["status"] = result.Status
                        });
                }

                _state.LastPresentedWithGpuTexture = false;
                UpdateRuntimeStatusFromResult(
                    result,
                    displayedPresentation,
                    requestStatus);
            }

            _session.Strokes.Publish(result.StrokeFrame);
            _session.NprFrames.Publish(result.NprFrame);
            _session.Debug.Publish(result.DebugFrame);
            _session.Commands.Execute(
                new ICommand[]
                {
                    new SetViewportSizeCommand(result.Diagnostics.Width, result.Diagnostics.Height),
                    new RequestRenderCommand()
                },
                log: false);
            _session.Workspace.Debug.RefreshFromEngine();
            _session.Workspace.Layers.RefreshRuntimeCounters();

            LogFrameIfNeeded(result);
            if (_state.LastPresentedWithGpuTexture is false && result.OutputKind != NprRenderOutputKind.GpuTexture)
            {
                // Already updated from branch above.
            }
            return true;
        }
    }

    private void OnRenderCompleted(NprRenderResult result)
    {
        if (_disposed)
        {
            result.Dispose();
            return;
        }

        if (result.Status is NprRenderStatus.Cancelled or NprRenderStatus.Dropped)
        {
            var requestNextFrame = false;
            lock (_gate)
            {
                _state.LastCompletedRevision = NumericMath.AtLeast(_state.LastCompletedRevision, result.Revision);
                _state.CleanupRuntimeStatuses(_state.LastCompletedRevision);
                _state.RenderInFlight = _state.LastCompletedRevision < _state.LastEnqueuedRevision;
                requestNextFrame = _state.DeferredFrameRequested && !_state.RenderInFlight;
            }

            result.Dispose();
            if (requestNextFrame)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (!_disposed)
                    {
                        _requestPresent();
                    }
                }, DispatcherPriority.Render);
            }

            return;
        }

        var requestDeferredFrame = false;
        lock (_gate)
        {
            _state.LastCompletedRevision = NumericMath.AtLeast(_state.LastCompletedRevision, result.Revision);
            _state.CleanupRuntimeStatuses(_state.LastCompletedRevision - 1);
            _state.RenderInFlight = _state.LastCompletedRevision < _state.LastEnqueuedRevision;
            requestDeferredFrame = _state.DeferredFrameRequested && !_state.RenderInFlight;
            _pendingResult?.Dispose();
            _pendingResult = result;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (_disposed)
            {
                lock (_gate)
                {
                    _pendingResult?.Dispose();
                    _pendingResult = null;
                }

                return;
            }

            _requestPresent();
            if (requestDeferredFrame)
            {
                _requestNextFrame();
            }
        }, DispatcherPriority.Render);
    }

    private void UpdateDefaultDrawProgress()
    {
        var presetState = _session.ActivePreset;
        if (!string.Equals(presetState.ActivePreset.PipelineId, NprPipelineIds.Default, StringComparison.OrdinalIgnoreCase))
        {
            _lastDrawTick = DateTimeOffset.Now;
            return;
        }

        var drawing = presetState.ActiveSettings.DefaultDrawing;
        var now = DateTimeOffset.Now;
        var deltaTime = NumericMath.AtMost((float)(now - _lastDrawTick).TotalSeconds, 0.05f);
        _lastDrawTick = now;

        if (drawing.AutoDraw)
        {
            drawing.DrawProgress = NumericMath.Clamp01(drawing.DrawProgress + deltaTime * drawing.DrawSpeed);
        }
    }

    private void UpdateRuntimeStatusFromResult(
        NprRenderResult result,
        bool presentedDirect,
        ViewportRuntimeStatus requestStatus)
    {
        var drawBitmap = result.OutputKind == NprRenderOutputKind.PixelSurface;
        var outputKind = result.OutputKind.ToString();
        var adapterName = string.IsNullOrWhiteSpace(result.Diagnostics.Notes)
            ? requestStatus.AdapterName
            : result.Diagnostics.Notes;
        var gpuReadbackMs = (float)(result.Diagnostics.Timings.FirstOrDefault(t => t.Name == "GpuReadback")?.Milliseconds ?? 0d);

        var effectivePresentation = result.OutputKind == NprRenderOutputKind.GpuTexture && presentedDirect
            ? "Direct"
            : result.OutputKind == NprRenderOutputKind.GpuTexture
                ? "DirectSkipped"
                : requestStatus.EffectivePresentation;

        _session.Workspace.Renderer.UpdateRuntimeStatus(
            requestStatus.EffectiveBackend,
            requestStatus.EffectiveApi,
            effectivePresentation,
            requestStatus.SurfaceMode,
            requestStatus.DirectPresenterAvailable,
            requestStatus.DirectSuppressed,
            requestStatus.PreferGpuPresentation,
            requestStatus.RequireGpuReadback,
            requestStatus.AllowGpuReadback,
            requestStatus.ShowDirectHost,
            drawBitmap,
            adapterName,
            $"{requestStatus.StatusMessage}{(gpuReadbackMs > 0 ? $" | GpuReadback {gpuReadbackMs:0.00}ms" : string.Empty)}",
            outputKind,
            gpuReadbackMs);
    }

    private void LogFrameIfNeeded(NprRenderResult result)
    {
        if (result.Revision != 1 && result.Revision - _state.LastLoggedRevision < 120)
        {
            return;
        }

        _state.LastLoggedRevision = result.Revision;
        LogProcessMemory(result);
        var pipelineMs = result.Diagnostics.Timings.FirstOrDefault(t => t.Name == "NprPipeline.Execute")?.Milliseconds ?? 0;
        if (result.ExecutionProfile == NprExecutionProfile.CpuDrivenGpuAccelerated)
        {
            var gpuClearMs = result.Diagnostics.Timings.FirstOrDefault(t => t.Name == "GpuClear")?.Milliseconds ?? 0;
            var gpuStrokeMs = result.Diagnostics.Timings.Where(t => t.Name == "GpuStrokeDraw").Sum(t => t.Milliseconds);
            var gpuToneMs = result.Diagnostics.Timings.Where(t => t.Name == "GpuToneSurfaceDraw").Sum(t => t.Milliseconds);
            var gpuReadbackMs = result.Diagnostics.Timings.FirstOrDefault(t => t.Name == "GpuReadback")?.Milliseconds ?? 0;
            var gpuDebugMs = result.Diagnostics.Timings.Where(t => t.Name == "GpuDebugOverlayBuild" || t.Name == "GpuDebugOverlayDraw").Sum(t => t.Milliseconds);
            StfuUiLog.Write(
                $"GPU frame {result.Revision}: total={result.Diagnostics.TotalMilliseconds:0.00}ms " +
                $"pipeline={pipelineMs:0.00}ms clear={gpuClearMs:0.00}ms strokes={gpuStrokeMs:0.00}ms " +
                $"tones={gpuToneMs:0.00}ms debug={gpuDebugMs:0.00}ms readback={gpuReadbackMs:0.00}ms " +
                $"paths={result.Diagnostics.PathCount} layers={result.Diagnostics.LayerCount} " +
                $"tones={result.Diagnostics.ToneSurfaceCount} workers={result.Diagnostics.WorkerCount} " +
                $"mode={result.Diagnostics.WorkerBudgetMode} adapter={result.Diagnostics.Notes}");
            StfuLog.Write(
                StfuLogDomain.RenderGpu,
                "frame.completed",
                $"GPU frame {result.Revision}",
                properties: new Dictionary<string, object?>
                {
                    ["revision"] = result.Revision,
                    ["totalMs"] = result.Diagnostics.TotalMilliseconds,
                    ["pipelineMs"] = pipelineMs,
                    ["clearMs"] = gpuClearMs,
                    ["strokeMs"] = gpuStrokeMs,
                    ["toneMs"] = gpuToneMs,
                    ["debugMs"] = gpuDebugMs,
                    ["readbackMs"] = gpuReadbackMs,
                    ["paths"] = result.Diagnostics.PathCount,
                    ["layers"] = result.Diagnostics.LayerCount,
                    ["tones"] = result.Diagnostics.ToneSurfaceCount,
                    ["workers"] = result.Diagnostics.WorkerCount,
                    ["workerBudgetMode"] = result.Diagnostics.WorkerBudgetMode,
                    ["processorCount"] = result.Diagnostics.ProcessorCount,
                    ["adapter"] = result.Diagnostics.Notes
                });
            StfuLog.Write(
                StfuLogDomain.Perf,
                "render.gpu",
                $"revision={result.Revision}",
                properties: new Dictionary<string, object?>
                {
                    ["totalMs"] = result.Diagnostics.TotalMilliseconds,
                    ["pipelineMs"] = pipelineMs,
                    ["readbackMs"] = gpuReadbackMs,
                    ["workers"] = result.Diagnostics.WorkerCount,
                    ["workerBudgetMode"] = result.Diagnostics.WorkerBudgetMode
                });
            return;
        }

        var rasterMs = result.Diagnostics.Timings.FirstOrDefault(t => t.Name == "CpuRasterize")?.Milliseconds ?? 0;
        StfuUiLog.Write(
            $"Full CPU frame {result.Revision}: total={result.Diagnostics.TotalMilliseconds:0.00}ms " +
            $"pipeline={pipelineMs:0.00}ms raster={rasterMs:0.00}ms paths={result.Diagnostics.PathCount} " +
            $"layers={result.Diagnostics.LayerCount} tones={result.Diagnostics.ToneSurfaceCount} " +
            $"workers={result.Diagnostics.WorkerCount} mode={result.Diagnostics.WorkerBudgetMode}");
        StfuLog.Write(
            StfuLogDomain.RenderCpu,
            "frame.completed",
            $"Full CPU frame {result.Revision}",
            properties: new Dictionary<string, object?>
            {
                ["revision"] = result.Revision,
                ["totalMs"] = result.Diagnostics.TotalMilliseconds,
                ["pipelineMs"] = pipelineMs,
                ["rasterMs"] = rasterMs,
                ["paths"] = result.Diagnostics.PathCount,
                ["layers"] = result.Diagnostics.LayerCount,
                ["tones"] = result.Diagnostics.ToneSurfaceCount,
                ["workers"] = result.Diagnostics.WorkerCount,
                ["workerBudgetMode"] = result.Diagnostics.WorkerBudgetMode,
                ["processorCount"] = result.Diagnostics.ProcessorCount
            });
        StfuLog.Write(
            StfuLogDomain.Perf,
            "render.cpu",
            $"revision={result.Revision}",
            properties: new Dictionary<string, object?>
            {
                ["totalMs"] = result.Diagnostics.TotalMilliseconds,
                ["pipelineMs"] = pipelineMs,
                ["rasterMs"] = rasterMs,
                ["workers"] = result.Diagnostics.WorkerCount,
                ["workerBudgetMode"] = result.Diagnostics.WorkerBudgetMode
            });
    }

    private static void LogProcessMemory(NprRenderResult result)
    {
        using var process = Process.GetCurrentProcess();
        var gcInfo = GC.GetGCMemoryInfo();
        StfuLog.Write(
            StfuLogDomain.Memory,
            "viewport.presented",
            $"revision={result.Revision}",
            properties: new Dictionary<string, object?>
            {
                ["revision"] = result.Revision,
                ["profile"] = result.ExecutionProfile,
                ["output"] = result.OutputKind,
                ["workingSetMb"] = BufferSizingMath.ToMegabytes(process.WorkingSet64),
                ["privateMb"] = BufferSizingMath.ToMegabytes(process.PrivateMemorySize64),
                ["gcHeapMb"] = BufferSizingMath.ToMegabytes(GC.GetTotalMemory(false)),
                ["gcHeapSizeMb"] = BufferSizingMath.ToMegabytes(gcInfo.HeapSizeBytes),
                ["totalAllocatedMb"] = BufferSizingMath.ToMegabytes(GC.GetTotalAllocatedBytes(false)),
                ["allocatedFrameMb"] = BufferSizingMath.ToMegabytes(result.Diagnostics.AllocatedBytes),
                ["gen0"] = GC.CollectionCount(0),
                ["gen1"] = GC.CollectionCount(1),
                ["gen2"] = GC.CollectionCount(2)
            });
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _scheduler.RenderCompleted -= OnRenderCompleted;
        lock (_gate)
        {
            _pendingResult?.Dispose();
            _pendingResult = null;
        }
    }
}
