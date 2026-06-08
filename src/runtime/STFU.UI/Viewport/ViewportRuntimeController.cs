using System.ComponentModel;
using Avalonia.Controls;
using STFU.UI.Bridge.Renderer;

namespace STFU.UI;

internal sealed class ViewportRuntimeController : IDisposable
{
    private readonly EngineViewportControl _viewport;
    private readonly RendererSettingsViewModel _renderer;
    private readonly RendererRuntimePlanResolver _runtimePlanResolver = new();
    private readonly bool _hasGpuRenderer;
    private readonly string? _adapterName;
    private DirectXViewportHost? _directHost;
    private bool _disposed;

    public ViewportRuntimeController(
        EngineViewportControl viewport,
        RendererSettingsViewModel renderer,
        bool hasGpuRenderer,
        string? adapterName)
    {
        _viewport = viewport;
        _renderer = renderer;
        _hasGpuRenderer = hasGpuRenderer;
        _adapterName = adapterName;
        _renderer.PropertyChanged += OnRendererPropertyChanged;
        _viewport.PresentationStateChanged += OnViewportPresentationStateChanged;
    }

    public void AttachDirectHost(DirectXViewportHost directHost)
    {
        _directHost = directHost;
        _viewport.SetDirectSurfaceSizeProvider(() => (directHost.PixelWidth, directHost.PixelHeight));
        ApplySurfaceState(requestFrame: false);
    }

    public void ApplyStartupState()
    {
        ApplySurfaceState(requestFrame: true);
    }

    private void OnRendererPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (!IsRuntimeRelevantRendererProperty(args.PropertyName))
        {
            return;
        }

        if (args.PropertyName is nameof(RendererSettingsViewModel.PresentationPreference))
        {
            _viewport.ResetDirectPresentationFallback();
        }

        ApplySurfaceState(requestFrame: true);
    }

    private void OnViewportPresentationStateChanged()
    {
        ApplySurfaceState(requestFrame: true);
    }

    private void ApplySurfaceState(bool requestFrame)
    {
        var plan = ResolvePlan();
        var directRequested = _directHost is not null &&
            plan.SurfaceMode is ViewportSurfaceMode.DirectCandidate or ViewportSurfaceMode.DirectActive;
        _viewport.ApplyRuntimePlan(plan);

        var mode = plan.SurfaceMode;
        var showDirectHost = _directHost is not null && plan.ShowDirectHost;
        if (_directHost is not null)
        {
            _directHost.IsVisible = directRequested;
            _directHost.IsPresentationPumpEnabled = showDirectHost;
            _directHost.IsHitTestVisible = showDirectHost;
            _directHost.SetValue(Panel.ZIndexProperty, mode == ViewportSurfaceMode.DirectActive ? 10 : 0);
        }

        _viewport.SetValue(Panel.ZIndexProperty, mode == ViewportSurfaceMode.DirectActive ? 0 : 10);
        _viewport.IsHitTestVisible = !showDirectHost;
        _renderer.UpdateRuntimeStatus(
            plan.BackendLabel,
            plan.ApiLabel,
            plan.PresentationLabel,
            plan.SurfaceMode.ToString(),
            plan.DirectPresenterAvailable,
            plan.DirectSuppressed,
            plan.PreferGpuPresentation,
            plan.RequireGpuReadback,
            plan.AllowGpuReadback,
            plan.ShowDirectHost,
            plan.DrawBitmap,
            plan.AdapterLabel,
            plan.StatusMessage);

        if (requestFrame)
        {
            _viewport.RequestImmediateFrame();
        }
    }

    private RendererRuntimePlan ResolvePlan()
    {
        return _runtimePlanResolver.Resolve(
            _renderer,
            _hasGpuRenderer,
            _directHost is not null,
            _viewport.IsDirectPresentationSuppressed,
            _viewport.IsDirectGpuPresenting,
            _adapterName);
    }

    private static bool IsRuntimeRelevantRendererProperty(string? propertyName)
    {
        return propertyName is null or
            nameof(RendererSettingsViewModel.BackendPreference) or
            nameof(RendererSettingsViewModel.PresentationPreference) or
            nameof(RendererSettingsViewModel.ApiPreference) or
            nameof(RendererSettingsViewModel.MaxRenderWorkers) or
            nameof(RendererSettingsViewModel.EnableTileParallelism) or
            nameof(RendererSettingsViewModel.EnableGpuTimings) or
            nameof(RendererSettingsViewModel.WorkerBudgetMode);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _renderer.PropertyChanged -= OnRendererPropertyChanged;
        _viewport.PresentationStateChanged -= OnViewportPresentationStateChanged;
    }
}
