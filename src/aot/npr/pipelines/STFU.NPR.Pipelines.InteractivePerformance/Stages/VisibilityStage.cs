using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.NPR.Pipeline.InteractivePerformance.Providers;
using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;

namespace STFU.NPR.Pipeline.InteractivePerformance.Stages;

public sealed class VisibilityStage : IInteractivePipelineStage
{
    private readonly IInteractiveVisibilityProvider _provider;

    public VisibilityStage()
        : this(new CpuApproxVisibilityProvider())
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
        var key = new ArtifactKey(
            ArtifactKind.VisibleFaces,
            ContentHash: 0,
            CameraHash: 0,
            StyleHash: 0,
            Width: context.Intent.Width,
            Height: context.Intent.Height);

        if (context.Artifacts.TryGet<VisibleFaceSetArtifact>(key, out var cached))
        {
            context.Diagnostics.CacheHits++;
            context.Diagnostics.VisibleFaces = cached.VisibleFaceCount;
            return;
        }

        var artifact = _provider.BuildVisibleFaces(context);
        context.Artifacts.Set(artifact);

        context.Diagnostics.CacheMisses++;
        context.Diagnostics.VisibleFaces = artifact.VisibleFaceCount;
    }
}
