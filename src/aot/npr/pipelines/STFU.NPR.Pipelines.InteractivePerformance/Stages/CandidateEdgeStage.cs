using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;

namespace STFU.NPR.Pipeline.InteractivePerformance.Stages;

public sealed class CandidateEdgeStage : IInteractivePipelineStage
{
    public string Name => "InteractiveCandidateEdges";

    public bool ShouldRun(InteractiveFrameContext context)
    {
        return context.WorkClass is
            InteractiveWorkClass.StrokeCandidateRefresh or
            InteractiveWorkClass.FullVisibleStrokeRefresh;
    }

    public void Execute(InteractiveFrameContext context)
    {
        var key = new ArtifactKey(
            ArtifactKind.CandidateEdges,
            ContentHash: 0,
            CameraHash: 0,
            StyleHash: 0,
            Width: context.Intent.Width,
            Height: context.Intent.Height);

        if (context.Artifacts.TryGet<CandidateEdgeArtifact>(key, out var cached))
        {
            context.Diagnostics.CacheHits++;
            context.Diagnostics.CandidateEdges = cached.Edges.Length;
            return;
        }

        var artifact = new CandidateEdgeArtifact
        {
            Key = key,
            Revision = context.Intent.FrameId,
            LastBuildTime = TimeSpan.Zero,
            Edges = []
        };

        context.Artifacts.Set(artifact);
        context.Diagnostics.CacheMisses++;
        context.Diagnostics.CandidateEdges = artifact.Edges.Length;
    }
}
