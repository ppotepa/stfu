using STFU.NPR.Graph;
using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.NPR.Pipelines.Abstractions;

namespace STFU.NPR.Pipeline.InteractivePerformance.Providers;

public sealed class ProjectedTriangleVisibilityProvider : IInteractiveVisibilityProvider
{
    private readonly FramePipelineStrategyOptions _options;
    private readonly IInteractiveVisibilityProvider _fallbackProvider;

    public ProjectedTriangleVisibilityProvider(
        FramePipelineStrategyOptions options,
        IInteractiveVisibilityProvider fallbackProvider)
    {
        _options = options ?? FramePipelineStrategyOptions.Default;
        _fallbackProvider = fallbackProvider ?? new CpuReferenceVisibilityProvider();
    }

    public string Name => "ProjectedTriangleVisibility";

    public VisibleFaceSetArtifact BuildVisibleFaces(InteractiveFrameContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!_options.EnableProjectedTriangleVisibility)
        {
            return _fallbackProvider.BuildVisibleFaces(context);
        }

        if (!context.Artifacts.TryGetLatest(
                ArtifactKind.ProjectedTriangles,
                out ProjectedTriangleArtifact projected) ||
            projected.TriangleCount <= 0)
        {
            return _fallbackProvider.BuildVisibleFaces(context);
        }

        var visibleFaceIndices = BuildVisibleFaces(projected.Triangles, _options.RequireFrontFacingProjectedTriangleVisibility);
        var visibleCount = visibleFaceIndices.Length;

        var key = ArtifactKeyFactory.VisibleFaces(context.Intent, projected.TriangleCount);

        return new VisibleFaceSetArtifact
        {
            Key = key,
            Revision = context.Intent.FrameId,
            LastBuildTime = TimeSpan.Zero,
            FaceCount = projected.TriangleCount,
            VisibleFaceCount = visibleCount,
            VisibleFaceIndices = visibleFaceIndices,
            Source = InteractiveVisibilitySource.ProjectedTriangles,
            SourceProjectedTriangleCount = projected.TriangleCount,
            ProviderName = Name,
            Note = "Visibility derived from projected triangles from self-contained projection output."
        };
    }

    public static int[] BuildVisibleFaces(
        ReadOnlySpan<InteractiveProjectedTriangle> triangles,
        bool requireFrontFacing)
    {
        if (triangles.IsEmpty)
        {
            return [];
        }

        var visibleFaceIndices = new List<int>();
        for (var index = 0; index < triangles.Length; index++)
        {
            var triangle = triangles[index];
            if (!triangle.IsVisible)
            {
                continue;
            }

            if (requireFrontFacing && !triangle.IsFrontFacing)
            {
                continue;
            }

            visibleFaceIndices.Add(index);
        }

        return visibleFaceIndices.ToArray();
    }

    public static int[] BuildVisibleFaces(
        ReadOnlySpan<ProjectedTriangle> triangles,
        bool requireFrontFacing)
    {
        if (triangles.IsEmpty)
        {
            return [];
        }

        var visibleFaceIndices = new List<int>();
        for (var index = 0; index < triangles.Length; index++)
        {
            var triangle = triangles[index];
            if (!triangle.IsVisible)
            {
                continue;
            }

            if (requireFrontFacing && !triangle.IsFrontFacing)
            {
                continue;
            }

            visibleFaceIndices.Add(index);
        }

        return visibleFaceIndices.ToArray();
    }
}
