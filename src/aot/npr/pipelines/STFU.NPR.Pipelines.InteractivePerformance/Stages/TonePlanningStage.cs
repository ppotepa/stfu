using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;
using STFU.NPR.Pipelines.Abstractions;

namespace STFU.NPR.Pipeline.InteractivePerformance.Stages;

public sealed class TonePlanningStage : IInteractivePipelineStage
{
    private readonly FramePipelineStrategyOptions _options;

    public TonePlanningStage()
        : this(FramePipelineStrategyOptions.Default)
    {
    }

    public TonePlanningStage(FramePipelineStrategyOptions options)
    {
        _options = options ?? FramePipelineStrategyOptions.Default;
    }

    public string Name => "InteractiveTonePlanning";

    public bool ShouldRun(InteractiveFrameContext context)
    {
        if (_options.DeferToneCoverageWhenPreviewDoesNotRequireTone &&
            !_options.RequireToneCoverageForInteractivePreview &&
            context.WorkClass != InteractiveWorkClass.FullVisibleStrokeRefresh)
        {
            context.Diagnostics.TonePlanningDeferred = true;
            return false;
        }

        return context.WorkClass is
            InteractiveWorkClass.StrokeCandidateRefresh or
            InteractiveWorkClass.FullVisibleStrokeRefresh;
    }

    public void Execute(InteractiveFrameContext context)
    {
        var visibleFaces = LoadVisibleFaceSet(context);
        var sourceVisibleFaceCount = ResolveSourceVisibleFaceCount(context, visibleFaces);
        var totalFaceCount = visibleFaces?.FaceCount ?? context.ReferenceContext.Graph.Triangles.Count;
        var key = ArtifactKeyFactory.ToneCoverage(context.Intent, totalFaceCount, sourceVisibleFaceCount);

        if (context.Artifacts.TryGet<ToneCoverageArtifact>(key, out var cached))
        {
            context.Diagnostics.CacheHits++;
            WriteDiagnostics(context, cached);
            return;
        }

        var artifact = ToneCoveragePlanner.BuildCoverage(context, visibleFaces, key);

        context.Artifacts.Set(artifact);
        context.Diagnostics.CacheMisses++;
        WriteDiagnostics(context, artifact);
    }

    private static VisibleFaceSetArtifact? LoadVisibleFaceSet(InteractiveFrameContext context)
    {
        return context.Artifacts.TryGetLatest(ArtifactKind.VisibleFaces, out VisibleFaceSetArtifact visibleFaces)
            ? visibleFaces
            : null;
    }

    private static int ResolveSourceVisibleFaceCount(
        InteractiveFrameContext context,
        VisibleFaceSetArtifact? visibleFaces)
    {
        if (visibleFaces is not null)
        {
            return visibleFaces.VisibleFaceCount;
        }

        var graph = context.ReferenceContext.Graph;
        var faceVisible = graph.DefaultFaceIdVisibility?.FaceVisible;
        if (faceVisible is null || faceVisible.Length == 0)
        {
            return graph.Triangles.Count;
        }

        var limit = Math.Min(graph.Triangles.Count, faceVisible.Length);
        var count = 0;
        for (var i = 0; i < limit; i++)
        {
            if (faceVisible[i])
            {
                count++;
            }
        }

        return count;
    }

    private static void WriteDiagnostics(InteractiveFrameContext context, ToneCoverageArtifact artifact)
    {
        context.Diagnostics.TonePlanningDeferred = false;
        context.Diagnostics.ToneSourceFaces = artifact.SourceVisibleFaceCount;
        context.Diagnostics.ToneRegions = artifact.RegionCount;
        context.Diagnostics.ToneCoverageRatioPercent = artifact.CoverageRatioPercent;
        context.Diagnostics.ToneHighlightRegions = artifact.HighlightRegionCount;
        context.Diagnostics.ToneMidtoneRegions = artifact.MidtoneRegionCount;
        context.Diagnostics.ToneShadowRegions = artifact.ShadowRegionCount;
    }
}
