using STFU.NPR.Pipelines.Abstractions;
using STFU.Parallelism;

namespace STFU.UI.Bridge.Renderer;

public sealed record RendererSettingsSnapshot(
    RendererBackendPreference Backend = RendererBackendPreference.Auto,
    RendererApiPreference Api = RendererApiPreference.Auto,
    RendererPresentationPreference Presentation = RendererPresentationPreference.Auto,
    bool ShowRendererHud = true,
    bool EnableGpuTimings = true,
    WorkerBudgetMode WorkerBudgetMode = WorkerBudgetMode.Performance,
    int MaxRenderWorkers = 0,
    bool EnableTileParallelism = true,
    FramePipelineStrategy PipelineStrategy = FramePipelineStrategy.ReferenceQuality);
