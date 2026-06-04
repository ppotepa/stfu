using Avalonia.Threading;
using STFU.Messaging.Commands;
using STFU.NPR.Composition;
using STFU.NPR.Debug;
using STFU.Logging;
using STFU.Rendering.Abstractions.Execution;
using STFU.Rendering.Abstractions.Requests;
using STFU.Strokes;
using STFU.UI.Bridge.Renderer;
using STFU.UI.Bridge.Session;
using STFU.UI.Styling;
using STFU.Viewport;
using STFU.Viewport.Commands;

namespace STFU.UI;

internal sealed class ViewportRenderBridge : IDisposable
{
    private readonly UiEngineSession _session;
    private readonly INprRenderScheduler _scheduler;
    private readonly AvaloniaBitmapPresenter _bitmapPresenter;
    private readonly DirectXViewportPresenter? _directXPresenter;
    private readonly Action _requestPresent;
    private readonly object _gate = new();
    private NprRenderResult? _pendingResult;
    private long _revision;
    private long _lastLoggedRevision;
    private DateTimeOffset _lastDrawTick = DateTimeOffset.Now;
    private bool _renderInFlight;
    private bool _lastPresentedWithGpuTexture;
    private bool _disposed;

    public ViewportRenderBridge(
        UiEngineSession session,
        AvaloniaBitmapPresenter presenter,
        Action requestPresent,
        DirectXViewportPresenter? directXPresenter = null)
    {
        _session = session;
        _scheduler = session.RenderScheduler;
        _bitmapPresenter = presenter;
        _directXPresenter = directXPresenter;
        _requestPresent = requestPresent;
        _scheduler.RenderCompleted += OnRenderCompleted;
    }

    public bool IsDirectGpuPresenting => _lastPresentedWithGpuTexture && _directXPresenter?.IsAttached == true;

    public void RequestFrame(int width, int height, ViewportRenderMode viewportRenderMode)
    {
        if (_disposed)
        {
            return;
        }

        width = Math.Max(1, width);
        height = Math.Max(1, height);
        UpdateDefaultDrawProgress();

        if (_renderInFlight)
        {
            return;
        }

        _session.Workspace.Assets.TickAnimation();

        var revision = Interlocked.Increment(ref _revision);
        var contentKind = viewportRenderMode == ViewportRenderMode.Mesh
            ? NprRenderContentKind.MeshWireframe
            : NprRenderContentKind.NprPipeline;
        var renderer = _session.Workspace.Renderer;
        var executionProfile = ResolveExecutionProfile(renderer.BackendPreference);
        var useDirectGpuPresenter = ShouldUseDirectPresentation(renderer.PresentationPreference, executionProfile, out var presentationWarning);
        var runtimeStatus = BuildRuntimeStatus(renderer, executionProfile, useDirectGpuPresenter, presentationWarning);
        renderer.UpdateRuntimeStatus(
            runtimeStatus.EffectiveBackend,
            runtimeStatus.EffectiveApi,
            runtimeStatus.EffectivePresentation,
            runtimeStatus.AdapterName,
            runtimeStatus.StatusMessage);
        if (revision == 1 || revision % 120 == 0)
        {
            StfuLog.Write(
                StfuLogDomain.Viewport,
                "frame.request",
                viewportRenderMode.ToString(),
                properties: new Dictionary<string, object?>
                {
                    ["revision"] = revision,
                    ["width"] = width,
                    ["height"] = height,
                    ["profile"] = executionProfile,
                    ["presentation"] = runtimeStatus.EffectivePresentation
                });
        }

        var presetState = _session.ActivePreset;
        var request = new NprRenderRequest(
            Revision: revision,
            Width: width,
            Height: height,
            ExecutionProfile: executionProfile,
            ContentKind: contentKind,
            Scene: _session.Engine.Scene,
            Assets: _session.Assets,
            Camera: _session.CameraRig.Camera,
            Settings: presetState.ActiveSettings,
            Style: presetState.ActiveGrammar,
            StyleSet: presetState.ActiveStyleSet,
            EntityStyles: _session.EntityStyles,
            Analysis: _session.Analysis,
            FrameHistoryState: _session.FrameHistory,
            Pipeline: contentKind == NprRenderContentKind.NprPipeline ? presetState.ActivePipeline : null,
            ActivePresetId: presetState.ActivePreset.Metadata.Id,
            ActivePipelineId: presetState.ActivePreset.PipelineId,
            FrameId: _session.FrameHistory.PeekNextFrameId(),
            TimeSeconds: revision / 60f,
            PreviousFrame: _session.FrameHistory.GetPreviousFrame(),
            Quality: NprQualityProfile.Default,
            Budget: new NprFrameBudget(
                TargetFps: 60,
                MaxWorkerThreads: 0,
                AllowContinuousRendering: true,
                AllowDroppingOldFrames: true,
                EnableTileParallelism: true,
                TileSize: 32,
                RequireGpuReadback: executionProfile == NprExecutionProfile.CpuDrivenGpuAccelerated && !useDirectGpuPresenter,
                AllowGpuReadback: true,
                PreferGpuPresentation: useDirectGpuPresenter,
                EnableGpuDebugLayer: false,
                EnableGpuTiming: renderer.EnableGpuTimings),
            Theme: BuildTheme(),
            ShowGrid: _session.Workspace.Viewport.ShowGrid && viewportRenderMode == ViewportRenderMode.Mesh,
            IncludeDebugFrame: _session.Workspace.Viewport.DebugOverlay != DebugOverlayKind.None,
            DebugOverlay: _session.Workspace.Viewport.DebugOverlay);

        _renderInFlight = true;
        _ = _scheduler.EnqueueAsync(request);
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

                _lastPresentedWithGpuTexture = false;
                return false;
            }

            if (result.OutputKind == NprRenderOutputKind.GpuTexture)
            {
                if (_directXPresenter?.CanPresent(result) == true)
                {
                    _directXPresenter.Present(result);
                    _lastPresentedWithGpuTexture = true;
                }
                else
                {
                    StfuUiLog.Write("GPU texture result arrived without an attached DirectX presenter; frame skipped.");
                    StfuLog.Write(
                        StfuLogDomain.Viewport,
                        "gpu_texture.skipped",
                        "GPU texture result arrived without an attached DirectX presenter.",
                        StfuLogLevel.Warning,
                        new Dictionary<string, object?> { ["revision"] = result.Revision });
                    _lastPresentedWithGpuTexture = false;
                }
            }
            else
            {
                _bitmapPresenter.Present(result);
                _lastPresentedWithGpuTexture = false;
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
            UpdateRuntimeStatusFromResult(result);
            return true;
        }
    }

    private void OnRenderCompleted(NprRenderResult result)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_disposed)
            {
                result.Dispose();
                return;
            }

            _renderInFlight = false;
            lock (_gate)
            {
                _pendingResult?.Dispose();
                _pendingResult = result;
            }

            _requestPresent();
        });
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
        var deltaTime = Math.Min((float)(now - _lastDrawTick).TotalSeconds, 0.05f);
        _lastDrawTick = now;

        if (drawing.AutoDraw)
        {
            drawing.DrawProgress = Math.Clamp(drawing.DrawProgress + deltaTime * drawing.DrawSpeed, 0f, 1f);
        }
    }

    private NprExecutionProfile ResolveExecutionProfile(RendererBackendPreference backendPreference)
    {
        return backendPreference switch
        {
            RendererBackendPreference.FullCpu => NprExecutionProfile.FullCpuReference,
            RendererBackendPreference.CpuDrivenGpu when _session.HasGpuRenderer => NprExecutionProfile.CpuDrivenGpuAccelerated,
            RendererBackendPreference.CpuDrivenGpu => NprExecutionProfile.FullCpuReference,
            _ => _session.HasGpuRenderer
                ? NprExecutionProfile.CpuDrivenGpuAccelerated
                : NprExecutionProfile.FullCpuReference
        };
    }

    private bool ShouldUseDirectPresentation(
        RendererPresentationPreference presentationPreference,
        NprExecutionProfile executionProfile,
        out string warning)
    {
        warning = string.Empty;
        if (executionProfile != NprExecutionProfile.CpuDrivenGpuAccelerated)
        {
            return false;
        }

        var directAvailable = _directXPresenter?.IsAttached == true;
        if (presentationPreference == RendererPresentationPreference.Readback)
        {
            return false;
        }

        if (presentationPreference == RendererPresentationPreference.Direct)
        {
            if (directAvailable)
            {
                return true;
            }

            warning = "Direct presentation unavailable; using GPU readback.";
            return false;
        }

        return false;
    }

    private (string EffectiveBackend, string EffectiveApi, string EffectivePresentation, string AdapterName, string StatusMessage) BuildRuntimeStatus(
        RendererSettingsViewModel renderer,
        NprExecutionProfile executionProfile,
        bool useDirectGpuPresenter,
        string presentationWarning)
    {
        if (executionProfile == NprExecutionProfile.FullCpuReference)
        {
            var message = renderer.BackendPreference == RendererBackendPreference.CpuDrivenGpu && !_session.HasGpuRenderer
                ? "GPU backend unavailable; using Full CPU."
                : string.Empty;
            return ("CPU", "CPU", "Bitmap", "Unavailable", message);
        }

        var api = renderer.ApiPreference switch
        {
            RendererApiPreference.Auto or RendererApiPreference.DirectX11 => "DX11",
            RendererApiPreference.Vulkan => "DX11",
            RendererApiPreference.OpenGL => "DX11",
            RendererApiPreference.Direct3D12 => "DX11",
            _ => "DX11"
        };
        var apiWarning = renderer.ApiPreference switch
        {
            RendererApiPreference.Vulkan => "Vulkan is not implemented; using DirectX 11.",
            RendererApiPreference.OpenGL => "OpenGL is not implemented; using DirectX 11.",
            RendererApiPreference.Direct3D12 => "Direct3D 12 is not implemented; using DirectX 11.",
            _ => string.Empty
        };
        var statusMessage = string.IsNullOrWhiteSpace(presentationWarning) ? apiWarning : presentationWarning;
        return (
            "CPU+GPU",
            api,
            useDirectGpuPresenter ? "Direct" : "Readback",
            _session.GpuRenderBackend?.Info.Name ?? "DirectX D3D11",
            statusMessage);
    }

    private void UpdateRuntimeStatusFromResult(NprRenderResult result)
    {
        var renderer = _session.Workspace.Renderer;
        var isGpu = result.ExecutionProfile == NprExecutionProfile.CpuDrivenGpuAccelerated;
        var adapterName = result.Diagnostics.Notes ?? _session.GpuRenderBackend?.Info.Name ?? "Unavailable";
        var statusMessage = renderer.StatusMessage;
        if (!isGpu)
        {
            renderer.UpdateRuntimeStatus("CPU", "CPU", "Bitmap", "Unavailable", statusMessage);
            return;
        }

        renderer.UpdateRuntimeStatus(
            "CPU+GPU",
            "DX11",
            result.OutputKind == NprRenderOutputKind.GpuTexture ? "Direct" : "Readback",
            adapterName,
            statusMessage);
    }

    private void LogFrameIfNeeded(NprRenderResult result)
    {
        if (result.Revision != 1 && result.Revision - _lastLoggedRevision < 120)
        {
            return;
        }

        _lastLoggedRevision = result.Revision;
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
                $"tones={result.Diagnostics.ToneSurfaceCount} adapter={result.Diagnostics.Notes}");
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
                    ["readbackMs"] = gpuReadbackMs
                });
            return;
        }

        var rasterMs = result.Diagnostics.Timings.FirstOrDefault(t => t.Name == "CpuRasterize")?.Milliseconds ?? 0;
        StfuUiLog.Write(
            $"Full CPU frame {result.Revision}: total={result.Diagnostics.TotalMilliseconds:0.00}ms " +
            $"pipeline={pipelineMs:0.00}ms raster={rasterMs:0.00}ms paths={result.Diagnostics.PathCount} " +
            $"layers={result.Diagnostics.LayerCount} tones={result.Diagnostics.ToneSurfaceCount} workers={result.Diagnostics.WorkerCount}");
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
                ["workers"] = result.Diagnostics.WorkerCount
            });
        StfuLog.Write(
            StfuLogDomain.Perf,
            "render.cpu",
            $"revision={result.Revision}",
            properties: new Dictionary<string, object?>
            {
                ["totalMs"] = result.Diagnostics.TotalMilliseconds,
                ["pipelineMs"] = pipelineMs,
                ["rasterMs"] = rasterMs
            });
    }

    private static NprRenderTheme BuildTheme()
    {
        return UiThemeService.IsDark
            ? new NprRenderTheme(
                true,
                new StrokeColor(23, 25, 22),
                new StrokeColor(58, 64, 55),
                new StrokeColor(43, 48, 41),
                new StrokeColor(225, 229, 221))
            : new NprRenderTheme(
                false,
                new StrokeColor(245, 245, 242),
                new StrokeColor(215, 215, 210),
                new StrokeColor(232, 232, 228),
                StrokeColor.Black);
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
