using STFU.Abstractions.Modules;
using STFU.NPR.Analysis;
using STFU.NPR.Debug;
using STFU.NPR.Handlers;
using STFU.NPR.Rendering;
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

    public void Register(IModuleContext context)
    {
        var providers = _pipelineProviders.ToArray();

        var pipelines = new NprPipelineRegistry();
        foreach (var provider in providers)
        {
            pipelines.Register(provider);
        }

        var providerPresets = providers.SelectMany(provider => provider.CreateBuiltInPresets()).ToArray();
        if (providerPresets.Length == 0)
        {
            throw new InvalidOperationException("NPR module requires at least one built-in preset from a pipeline provider.");
        }

        var registry = new NprPresetRegistry(providerPresets[0]);
        foreach (var providerPreset in providerPresets.Skip(1))
        {
            registry.Register(providerPreset);
        }

        foreach (var additionalPreset in _additionalPresets)
        {
            registry.Register(additionalPreset);
        }

        var debug = new NprDebugState();
        var activePreset = new ActiveNprPresetState(registry, pipelines);
        var analysis = new MeshAnalysisCacheStore();
        var frameHistory = new FrameHistoryState();
        var entityStyles = new NprEntityStyleRegistry();
        var nprFrameState = new NprFrameState();

        context.Services.AddSingleton(registry);
        context.Services.AddSingleton(pipelines);
        context.Services.AddSingleton(activePreset);
        context.Services.AddSingleton(debug);
        context.Services.AddSingleton(analysis);
        context.Services.AddSingleton(frameHistory);
        context.Services.AddSingleton(entityStyles);
        context.Services.AddSingleton(nprFrameState);

        context.Commands.Register(new SetEntityNprRoleCommandHandler(entityStyles));
    }
}
