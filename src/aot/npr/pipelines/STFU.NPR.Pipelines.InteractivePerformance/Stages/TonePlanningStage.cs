using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;

namespace STFU.NPR.Pipeline.InteractivePerformance.Stages;

public sealed class TonePlanningStage : IInteractivePipelineStage
{
    public string Name => "InteractiveTonePlanning";

    public bool ShouldRun(InteractiveFrameContext context)
    {
        return context.WorkClass is InteractiveWorkClass.FullVisibleStrokeRefresh;
    }

    public void Execute(InteractiveFrameContext context)
    {
        var key = new ArtifactKey(
            ArtifactKind.ToneCoverage,
            ContentHash: 0,
            CameraHash: 0,
            StyleHash: 0,
            Width: context.Intent.Width,
            Height: context.Intent.Height);

        if (context.Artifacts.TryGet<ToneCoverageArtifact>(key, out _))
        {
            context.Diagnostics.CacheHits++;
            return;
        }

        var artifact = new ToneCoverageArtifact
        {
            Key = key,
            Revision = context.Intent.FrameId,
            LastBuildTime = TimeSpan.Zero,
            RegionCount = 0,
            Note = "Interactive tone coverage placeholder; real visible face grouping is not implemented yet."
        };

        context.Artifacts.Set(artifact);
        context.Diagnostics.CacheMisses++;
    }
}
