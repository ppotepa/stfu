using STFU.Engine.Composition;
using STFU.NPR.Analysis;
using STFU.NPR.Debug;
using STFU.NPR.Pipeline;
using STFU.NPR.Settings;
using STFU.NPR.Temporal;
using STFU.NPR.Visibility;

namespace STFU.NPR.Composition;

public sealed class NprModule : IEngineModule
{
    private readonly IReadOnlyList<INprPreset> _additionalPresets;

    public NprModule(params INprPreset[] additionalPresets)
    {
        _additionalPresets = additionalPresets;
    }

    public void Register(EngineModuleContext context)
    {
        INprPreset preset = new GenericSketchNprPreset();
        var registry = new NprPresetRegistry(preset);
        foreach (var additionalPreset in _additionalPresets)
        {
            registry.Register(additionalPreset);
        }

        var settings = preset.CreateSettings();
        var pipeline = preset.CreatePipeline();
        var debug = new NprDebugState();
        var grammar = preset.CreateGrammar();
        var activePreset = new ActiveNprPresetState(registry);
        var analysis = new MeshAnalysisCacheStore();
        IVisibilityResolver visibilityResolver = new BvhVisibilityResolver();
        IOcclusionQuery occlusionQuery = new BvhOcclusionQuery();
        var frameHistory = new FrameHistoryState();

        context.Services.AddSingleton(registry);
        context.Services.AddSingleton(activePreset);
        context.Services.AddSingleton(preset);
        context.Services.AddSingleton(settings);
        context.Services.AddSingleton(pipeline);
        context.Services.AddSingleton(debug);
        context.Services.AddSingleton(grammar);
        context.Services.AddSingleton(analysis);
        context.Services.AddSingleton(visibilityResolver);
        context.Services.AddSingleton(occlusionQuery);
        context.Services.AddSingleton(frameHistory);
    }
}
