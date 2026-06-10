using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;

namespace STFU.NPR.Pipeline.InteractivePerformance.Stages;

public sealed class ProjectionStage : IInteractivePipelineStage
{
    public string Name => "InteractiveProjection";

    public bool ShouldRun(InteractiveFrameContext context)
    {
        return context.WorkClass is
            InteractiveWorkClass.ProjectionOnly or
            InteractiveWorkClass.VisibilityRefresh or
            InteractiveWorkClass.StrokeCandidateRefresh or
            InteractiveWorkClass.FullVisibleStrokeRefresh;
    }

    public void Execute(InteractiveFrameContext context)
    {
        var key = ArtifactKeyFactory.ProjectionSummary(context.Intent);

        if (context.Artifacts.TryGet<ProjectionSummaryArtifact>(key, out _))
        {
            context.Diagnostics.CacheHits++;
            context.Diagnostics.ProjectedVertices = 0;
            return;
        }

        var artifact = new ProjectionSummaryArtifact
        {
            Key = key,
            Revision = context.Intent.FrameId,
            Width = context.Intent.Width,
            Height = context.Intent.Height,
            FullProjectionAvailable = false,
            LastBuildTime = TimeSpan.Zero,
            Note = "Interactive projection summary created; full projected vertex artifact is not implemented yet."
        };

        context.Artifacts.Set(artifact);

        context.Diagnostics.CacheMisses++;
        context.Diagnostics.ProjectedVertices = 0;
    }

}
