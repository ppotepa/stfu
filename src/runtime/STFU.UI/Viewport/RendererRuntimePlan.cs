using STFU.NPR.Pipelines.Abstractions;
using STFU.Rendering.Abstractions.Execution;
using STFU.UI.Bridge.Renderer;
using STFU.UI.Viewport;

namespace STFU.UI;

internal readonly record struct RendererSettingAvailability(
    bool CanSelectApi,
    bool CanSelectPresentation,
    bool CanEnableGpuTimings,
    bool CanUseDirectPresentation,
    string DisabledReason);

internal readonly record struct RendererRuntimePlan(
    RendererBackendPreference RequestedBackend,
    RendererPresentationPreference RequestedPresentation,
    FramePipelineStrategy PipelineStrategy,
    NprExecutionProfile EffectiveProfile,
    ViewportPresentationKind EffectivePresentation,
    ViewportSurfaceMode SurfaceMode,
    bool HasGpuRenderer,
    bool DirectPresenterAvailable,
    bool DirectSuppressed,
    bool PreferGpuPresentation,
    bool RequireGpuReadback,
    bool AllowGpuReadback,
    bool ShowDirectHost,
    bool DrawBitmap,
    bool IsFallback,
    string BackendLabel,
    string ApiLabel,
    string PresentationLabel,
    string AdapterLabel,
    string PipelineStrategyLabel,
    string PipelineStrategyStatus,
    string StatusMessage)
{
    public RendererSettingAvailability SettingAvailability => EffectiveProfile == NprExecutionProfile.FullCpuReference
        ? new RendererSettingAvailability(
            CanSelectApi: false,
            CanSelectPresentation: false,
            CanEnableGpuTimings: false,
            CanUseDirectPresentation: false,
            DisabledReason: "GPU settings are disabled for Full CPU backend.")
        : new RendererSettingAvailability(
            CanSelectApi: true,
            CanSelectPresentation: true,
            CanEnableGpuTimings: true,
            CanUseDirectPresentation: DirectPresenterAvailable,
            DisabledReason: DirectPresenterAvailable ? string.Empty : "Direct GPU presentation is unavailable.");
}

internal sealed class RendererRuntimePlanResolver
{
    public RendererRuntimePlan Resolve(
        RendererSettingsViewModel settings,
        bool hasGpuRenderer,
        bool directPresenterAvailable,
        bool directSuppressed,
        bool directPresenting,
        string? adapterName)
    {
        var pipelineStrategy = settings.PipelineStrategy;
        var strategyLabel = FramePipelineStrategyDisplay.GetDisplayName(pipelineStrategy);
        var strategyStatus = FramePipelineStrategyDisplay.GetStatusNote(pipelineStrategy);

        var effectiveProfile = ResolveExecutionProfile(settings.BackendPreference, hasGpuRenderer);
        if (effectiveProfile == NprExecutionProfile.FullCpuReference)
        {
            return new RendererRuntimePlan(
                settings.BackendPreference,
                settings.PresentationPreference,
                pipelineStrategy,
                effectiveProfile,
                ViewportPresentationKind.Bitmap,
                ViewportSurfaceMode.Bitmap,
                hasGpuRenderer,
                directPresenterAvailable,
                directSuppressed,
                PreferGpuPresentation: false,
                RequireGpuReadback: false,
                AllowGpuReadback: true,
                ShowDirectHost: false,
                DrawBitmap: true,
                IsFallback: settings.BackendPreference == RendererBackendPreference.CpuDrivenGpu && !hasGpuRenderer,
                BackendLabel: "CPU",
                ApiLabel: "CPU",
                PresentationLabel: "Bitmap",
                AdapterLabel: "Unavailable",
                PipelineStrategyLabel: strategyLabel,
                PipelineStrategyStatus: strategyStatus,
                StatusMessage: ResolveCpuStatus(settings.BackendPreference, hasGpuRenderer));
        }

        var apiLabel = ResolveApiLabel(settings.ApiPreference);
        var apiWarning = ResolveApiWarning(settings.ApiPreference);
        var requestedDirect = settings.PresentationPreference == RendererPresentationPreference.Direct ||
            settings.PresentationPreference == RendererPresentationPreference.Auto && directPresenterAvailable;

        if (pipelineStrategy == FramePipelineStrategy.InteractivePerformance && directPresenterAvailable)
        {
            requestedDirect = true;
        }

        if (settings.PresentationPreference == RendererPresentationPreference.Readback &&
            pipelineStrategy != FramePipelineStrategy.InteractivePerformance)
        {
            requestedDirect = false;
        }

        if (!requestedDirect)
        {
            return CreateGpuReadbackPlan(settings, pipelineStrategy, hasGpuRenderer, directPresenterAvailable, directSuppressed, apiLabel, adapterName, apiWarning, isFallback: false);
        }

        if (!directPresenterAvailable)
        {
            return CreateGpuReadbackPlan(
                settings,
                pipelineStrategy,
                hasGpuRenderer,
                directPresenterAvailable,
                directSuppressed,
                apiLabel,
                adapterName,
                "Direct presentation unavailable; using GPU readback.",
                isFallback: true);
        }

        if (directSuppressed)
        {
            return CreateGpuReadbackPlan(
                settings,
                pipelineStrategy,
                hasGpuRenderer,
                directPresenterAvailable,
                directSuppressed,
                apiLabel,
                adapterName,
                "Direct GPU presentation is suppressed after failures; using GPU readback fallback.",
                isFallback: true);
        }

        var surfaceMode = directPresenting
            ? ViewportSurfaceMode.DirectActive
            : ViewportSurfaceMode.DirectCandidate;
        return new RendererRuntimePlan(
            settings.BackendPreference,
            settings.PresentationPreference,
            pipelineStrategy,
            effectiveProfile,
            ViewportPresentationKind.DirectGpu,
            surfaceMode,
            hasGpuRenderer,
            directPresenterAvailable,
            directSuppressed,
            PreferGpuPresentation: true,
            RequireGpuReadback: false,
            AllowGpuReadback: false,
            ShowDirectHost: surfaceMode == ViewportSurfaceMode.DirectActive,
            DrawBitmap: surfaceMode != ViewportSurfaceMode.DirectActive,
            IsFallback: false,
            BackendLabel: "CPU+GPU",
            ApiLabel: apiLabel,
            PresentationLabel: surfaceMode == ViewportSurfaceMode.DirectActive ? "Direct" : "Direct pending",
            AdapterLabel: string.IsNullOrWhiteSpace(adapterName) ? "Unavailable" : adapterName,
            PipelineStrategyLabel: strategyLabel,
            PipelineStrategyStatus: strategyStatus,
            StatusMessage: string.IsNullOrWhiteSpace(apiWarning) && surfaceMode == ViewportSurfaceMode.DirectCandidate
                ? "Direct GPU pending first successful present."
                : apiWarning);
    }

    private static RendererRuntimePlan CreateGpuReadbackPlan(
        RendererSettingsViewModel settings,
        FramePipelineStrategy pipelineStrategy,
        bool hasGpuRenderer,
        bool directPresenterAvailable,
        bool directSuppressed,
        string apiLabel,
        string? adapterName,
        string statusMessage,
        bool isFallback)
    {
        return new RendererRuntimePlan(
            settings.BackendPreference,
            settings.PresentationPreference,
            pipelineStrategy,
            NprExecutionProfile.CpuDrivenGpuAccelerated,
            ViewportPresentationKind.Bitmap,
            isFallback ? ViewportSurfaceMode.DirectSuppressed : ViewportSurfaceMode.Bitmap,
            hasGpuRenderer,
            directPresenterAvailable,
            directSuppressed,
            PreferGpuPresentation: false,
            RequireGpuReadback: true,
            AllowGpuReadback: true,
            ShowDirectHost: false,
            DrawBitmap: true,
            IsFallback: isFallback,
            BackendLabel: "CPU+GPU",
            ApiLabel: apiLabel,
            PresentationLabel: isFallback ? "Readback fallback" : "Readback",
            AdapterLabel: string.IsNullOrWhiteSpace(adapterName) ? "Unavailable" : adapterName,
            PipelineStrategyLabel: FramePipelineStrategyDisplay.GetDisplayName(pipelineStrategy),
            PipelineStrategyStatus: FramePipelineStrategyDisplay.GetStatusNote(pipelineStrategy),
            StatusMessage: statusMessage);
    }

    private static NprExecutionProfile ResolveExecutionProfile(RendererBackendPreference backendPreference, bool hasGpuRenderer)
    {
        return backendPreference switch
        {
            RendererBackendPreference.FullCpu => NprExecutionProfile.FullCpuReference,
            RendererBackendPreference.CpuDrivenGpu when hasGpuRenderer => NprExecutionProfile.CpuDrivenGpuAccelerated,
            RendererBackendPreference.CpuDrivenGpu => NprExecutionProfile.FullCpuReference,
            _ => hasGpuRenderer
                ? NprExecutionProfile.CpuDrivenGpuAccelerated
                : NprExecutionProfile.FullCpuReference
        };
    }

    private static string ResolveCpuStatus(RendererBackendPreference backendPreference, bool hasGpuRenderer)
    {
        if (backendPreference == RendererBackendPreference.CpuDrivenGpu && !hasGpuRenderer)
        {
            return "GPU backend unavailable; using Full CPU.";
        }

        if (backendPreference == RendererBackendPreference.FullCpu)
        {
            return "Full CPU backend selected; GPU presentation settings are disabled.";
        }

        return string.Empty;
    }

    private static string ResolveApiLabel(RendererApiPreference apiPreference)
    {
        return apiPreference switch
        {
            RendererApiPreference.Auto or RendererApiPreference.DirectX11 => "DX11",
            RendererApiPreference.Vulkan => "DX11",
            RendererApiPreference.OpenGL => "DX11",
            RendererApiPreference.Direct3D12 => "DX11",
            _ => "DX11"
        };
    }

    private static string ResolveApiWarning(RendererApiPreference apiPreference)
    {
        return apiPreference switch
        {
            RendererApiPreference.Vulkan => "Vulkan is not implemented; using DirectX 11.",
            RendererApiPreference.OpenGL => "OpenGL is not implemented; using DirectX 11.",
            RendererApiPreference.Direct3D12 => "Direct3D 12 is not implemented; using DirectX 11.",
            _ => string.Empty
        };
    }
}
