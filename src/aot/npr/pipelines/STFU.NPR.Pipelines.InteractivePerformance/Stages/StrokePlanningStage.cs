using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;

namespace STFU.NPR.Pipeline.InteractivePerformance.Stages;

public sealed class StrokePlanningStage : IInteractivePipelineStage
{
    public string Name => "InteractiveStrokePlanning";

    public bool ShouldRun(InteractiveFrameContext context)
    {
        return context.WorkClass is InteractiveWorkClass.FullVisibleStrokeRefresh;
    }

    public void Execute(InteractiveFrameContext context)
    {
        var key = new ArtifactKey(
            ArtifactKind.StrokeCommands,
            ContentHash: 0,
            CameraHash: 0,
            StyleHash: 0,
            Width: context.Intent.Width,
            Height: context.Intent.Height);

        if (context.Artifacts.TryGet<StrokeCommandArtifact>(key, out var cached))
        {
            context.Diagnostics.CacheHits++;
            context.Diagnostics.StrokeCommands = cached.Commands.Length;
            return;
        }

        var artifact = new StrokeCommandArtifact
        {
            Key = key,
            Revision = context.Intent.FrameId,
            LastBuildTime = TimeSpan.Zero,
            Commands = []
        };

        context.Artifacts.Set(artifact);
        context.Diagnostics.CacheMisses++;
        context.Diagnostics.StrokeCommands = artifact.Commands.Length;
    }
}
