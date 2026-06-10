using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.NPR.Pipeline.InteractivePerformance.Providers;
using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;
using STFU.NPR.Pipelines.Abstractions;

namespace STFU.NPR.Pipeline.InteractivePerformance.Stages;

public sealed class VisibilityStage : IInteractivePipelineStage
{
    private readonly IInteractiveVisibilityProvider _provider;

    public VisibilityStage()
        : this(FramePipelineStrategyOptions.Default)
    {
    }

    public VisibilityStage(FramePipelineStrategyOptions options)
        : this(new ProjectedTriangleVisibilityProvider(options, new CpuReferenceVisibilityProvider()))
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
        var faceCount = ResolveFaceCount(context);
        var key = ArtifactKeyFactory.VisibleFaces(context.Intent, faceCount);

        if (context.Artifacts.TryGet<VisibleFaceSetArtifact>(key, out var cached))
        {
            context.Diagnostics.CacheHits++;
            WriteDiagnostics(context, cached);
            return;
        }

        var artifact = _provider.BuildVisibleFaces(context);
        context.Artifacts.Set(artifact);

        context.Diagnostics.CacheMisses++;
        WriteDiagnostics(context, artifact);
    }

    private static int ResolveFaceCount(InteractiveFrameContext context)
    {
        if (context.Artifacts.TryGetLatest(ArtifactKind.ProjectedTriangles, out ProjectedTriangleArtifact projected) &&
            projected.TriangleCount > 0)
        {
            return projected.TriangleCount;
        }

        return context.ReferenceContext.Graph.Triangles.Count;
    }

    private static void WriteDiagnostics(InteractiveFrameContext context, VisibleFaceSetArtifact artifact)
    {
        context.Diagnostics.TotalFaces = artifact.FaceCount;
        context.Diagnostics.VisibleFaces = artifact.VisibleFaceCount;
        context.Diagnostics.VisibleFaceRatioPercent = artifact.VisibleFaceRatioPercent;
        context.Diagnostics.VisibilitySource = (long)artifact.Source;
        context.Diagnostics.VisibilityProviderName = artifact.ProviderName;
        context.Diagnostics.VisibilitySourceProjectedTriangles = artifact.SourceProjectedTriangleCount;
    }
}
