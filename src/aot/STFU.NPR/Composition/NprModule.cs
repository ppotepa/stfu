using STFU.Engine.Composition;
using STFU.NPR.Analysis;
using STFU.NPR.Debug;
using STFU.NPR.Handlers;
using STFU.NPR.Pipeline;
using STFU.NPR.Rendering;
using STFU.NPR.Settings;
using STFU.NPR.Temporal;

namespace STFU.NPR.Composition;

public sealed class NprModule : IEngineModule
{
    private readonly IReadOnlyList<INprPreset> _additionalPresets;
    private readonly IReadOnlyList<INprPipelineProvider> _pipelineProviders;

    public NprModule(params INprPreset[] additionalPresets)
        : this(additionalPresets, [])
    {
    }

    public NprModule(
        IReadOnlyList<INprPreset> additionalPresets,
        IReadOnlyList<INprPipelineProvider> pipelineProviders)
    {
        _additionalPresets = additionalPresets;
        _pipelineProviders = pipelineProviders;
    }

    public void Register(EngineModuleContext context)
    {
        INprPreset preset = new GenericSketchNprPreset();
        var registry = new NprPresetRegistry(preset);
        foreach (var additionalPreset in _additionalPresets)
        {
            registry.Register(additionalPreset);
        }

        var pipelines = new NprPipelineRegistry();
        pipelines.Register(new SketchPipelineProvider());
        foreach (var provider in _pipelineProviders)
        {
            pipelines.Register(provider);
        }

        var settings = preset.CreateSettings();
        var pipeline = preset.CreatePipeline();
        var debug = new NprDebugState();
        var grammar = preset.CreateGrammar();
        var activePreset = new ActiveNprPresetState(registry, pipelines);
        var analysis = new MeshAnalysisCacheStore();
        var frameHistory = new FrameHistoryState();
        var entityStyles = new NprEntityStyleRegistry();
        var nprFrameState = new NprFrameState();

        context.Services.AddSingleton(registry);
        context.Services.AddSingleton(pipelines);
        context.Services.AddSingleton(activePreset);
        context.Services.AddSingleton(preset);
        context.Services.AddSingleton(settings);
        context.Services.AddSingleton(pipeline);
        context.Services.AddSingleton(debug);
        context.Services.AddSingleton(grammar);
        context.Services.AddSingleton(analysis);
        context.Services.AddSingleton(frameHistory);
        context.Services.AddSingleton(entityStyles);
        context.Services.AddSingleton(nprFrameState);

        context.Commands.Register(new SetEntityNprRoleCommandHandler(entityStyles));
    }
}
