namespace STFU.UI.Bridge.Renderer;

public sealed record RendererSettingsSnapshot(
    RendererBackendPreference Backend = RendererBackendPreference.Auto,
    RendererApiPreference Api = RendererApiPreference.Auto,
    RendererPresentationPreference Presentation = RendererPresentationPreference.Auto,
    bool ShowRendererHud = true,
    bool EnableGpuTimings = true);
