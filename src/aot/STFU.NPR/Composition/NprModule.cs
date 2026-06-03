using STFU.Engine.Composition;
using STFU.NPR.Pipeline;
using STFU.NPR.Settings;

namespace STFU.NPR.Composition;

public sealed class NprModule : IEngineModule
{
    public void Register(EngineModuleContext context)
    {
        INprPreset preset = new GenericSketchNprPreset();
        var registry = new NprPresetRegistry(preset);
        var settings = preset.CreateSettings();
        var pipeline = preset.CreatePipeline();

        context.Services.AddSingleton(registry);
        context.Services.AddSingleton(preset);
        context.Services.AddSingleton(settings);
        context.Services.AddSingleton(pipeline);
    }
}
