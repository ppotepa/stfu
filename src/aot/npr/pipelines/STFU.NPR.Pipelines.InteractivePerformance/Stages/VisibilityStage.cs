using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.NPR.Pipeline.InteractivePerformance.Providers;
using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;

namespace STFU.NPR.Pipeline.InteractivePerformance.Stages;

public sealed class VisibilityStage : IInteractivePipelineStage
{
    private readonly IInteractiveVisibilityProvider _provider;

    public VisibilityStage()
        : this(new CpuReferenceVisibilityProvider())
    {
    }

    public VisibilityStage(IInteractiveVisibilityProvider provider)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public string Name => "InteractiveVisibility";

    public bool ShouldRun(InteractiveFrameContext context)
    {
        return context.WorkClass is
            InteractiveWorkClass.VisibilityRefresh or
            InteractiveWorkClass.StrokeCandidateRefresh or
            InteractiveWorkClass.FullVisibleStrokeRefresh;
    }

    public void Execute(InteractiveFrameContext context)
    {
        var faceCount = context.ReferenceContext.Graph.Triangles.Count;
        var key = ArtifactKeyFactory.VisibleFaces(context.Intent, faceCount);

        if (context.Artifacts.TryGet<VisibleFaceSetArtifact>(key, out var cached))
        {
            context.Diagnostics.CacheHits++;
            context.Diagnostics.TotalFaces = cached.FaceCount;
            context.Diagnostics.VisibleFaces = cached.VisibleFaceCount;
            context.Diagnostics.VisibleFaceRatioPercent = cached.VisibleFaceRatioPercent;
            return;
        }

        var artifact = _provider.BuildVisibleFaces(context);
        context.Artifacts.Set(artifact);

        context.Diagnostics.CacheMisses++;
        context.Diagnostics.TotalFaces = artifact.FaceCount;
        context.Diagnostics.VisibleFaces = artifact.VisibleFaceCount;
        context.Diagnostics.VisibleFaceRatioPercent = artifact.VisibleFaceRatioPercent;
    }
}
