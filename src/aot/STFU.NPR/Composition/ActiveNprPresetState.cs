using STFU.NPR.Pipeline;
using STFU.NPR.Settings;

namespace STFU.NPR.Composition;

public sealed class ActiveNprPresetState
{
    private readonly NprPresetRegistry _registry;
    private readonly NprPipelineRegistry _pipelines;

    public ActiveNprPresetState(NprPresetRegistry registry, NprPipelineRegistry pipelines)
    {
        _registry = registry;
        _pipelines = pipelines;
        ApplyPreset(registry.ActivePreset.Metadata.Id);
    }

    public INprPreset ActivePreset { get; private set; } = null!;

    public NprSettings ActiveSettings { get; private set; } = null!;

    public StyleGrammar ActiveGrammar { get; private set; } = null!;

    public NprStyleSet ActiveStyleSet { get; private set; } = null!;

    public INprPipeline ActivePipeline { get; private set; } = null!;

    public void ApplyPreset(string id)
    {
        _registry.SetActive(id);

        var preset = _registry.ActivePreset;
        ActivePreset = preset;
        ActiveSettings = preset.CreateSettings();
        ActiveGrammar = preset.CreateGrammar();
        ActiveStyleSet = preset.CreateStyleSet();
        if (!_pipelines.TryCreate(preset.PipelineId, out var pipeline))
        {
            throw new InvalidOperationException($"NPR pipeline is not registered: {preset.PipelineId}");
        }

        ActivePipeline = pipeline;
    }
}
