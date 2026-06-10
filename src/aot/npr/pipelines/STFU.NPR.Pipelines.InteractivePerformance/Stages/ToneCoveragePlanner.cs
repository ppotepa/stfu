using STFU.NPR.Graph;
using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;

namespace STFU.NPR.Pipeline.InteractivePerformance.Stages;

public static class ToneCoveragePlanner
{
    public static ToneCoverageArtifact BuildCoverage(
        InteractiveFrameContext context,
        VisibleFaceSetArtifact? visibleFaces,
        ArtifactKey key)
    {
        ArgumentNullException.ThrowIfNull(context);

        var sourceFaces = SelectSourceFaces(context.ReferenceContext.Graph, visibleFaces);
        var regions = BuildRegions(
            context.ReferenceContext.Graph.Triangles,
            sourceFaces,
            context.Intent.QualityMode);
        var (highlight, midtone, shadow) = CountBuckets(regions);

        return new ToneCoverageArtifact
        {
            Key = key,
            Revision = context.Intent.FrameId,
            LastBuildTime = TimeSpan.Zero,
            SourceVisibleFaceCount = sourceFaces.Length,
            HighlightRegionCount = highlight,
            MidtoneRegionCount = midtone,
            ShadowRegionCount = shadow,
            Regions = regions,
            Note = $"Interactive tone coverage from visible faces; quality={context.Intent.QualityMode}, regions={regions.Length}."
        };
    }

    public static InteractiveToneRegion[] BuildRegions(
        IReadOnlyList<ProjectedTriangle> triangles,
        IReadOnlyList<int> sourceFaceIndices,
        InteractiveQualityMode qualityMode)
    {
        ArgumentNullException.ThrowIfNull(triangles);
        ArgumentNullException.ThrowIfNull(sourceFaceIndices);

        if (triangles.Count == 0 || sourceFaceIndices.Count == 0)
        {
            return [];
        }

        var stride = ResolveFaceStride(qualityMode);
        var maxRegions = ResolveMaxRegions(qualityMode);
        var regions = new List<InteractiveToneRegion>(Math.Min(sourceFaceIndices.Count, maxRegions));

        for (var i = 0; i < sourceFaceIndices.Count && regions.Count < maxRegions; i += stride)
        {
            var faceIndex = sourceFaceIndices[i];
            if ((uint)faceIndex >= (uint)triangles.Count)
            {
                continue;
            }

            var triangle = triangles[faceIndex];
            if (!ShouldEmitToneRegion(triangle))
            {
                continue;
            }

            var shade = Math.Clamp(triangle.Shade, 0f, 1f);
            var bucket = ClassifyBucket(shade);
            regions.Add(new InteractiveToneRegion(
                SourceFaceId: faceIndex,
                ProjectedMeshIndex: triangle.ProjectedMeshIndex,
                MeshTriangleIndex: triangle.MeshTriangleIndex,
                Bucket: bucket,
                ScreenCenterX: triangle.ScreenCenter.X,
                ScreenCenterY: triangle.ScreenCenter.Y,
                ScreenArea: triangle.ScreenArea,
                Depth: triangle.Depth,
                Shade: shade,
                CoverageOpacity: ComputeCoverageOpacity(bucket, shade)));
        }

        return regions.ToArray();
    }

    private static int[] SelectSourceFaces(NprGraph graph, VisibleFaceSetArtifact? visibleFaces)
    {
        if (visibleFaces is not null)
        {
            return visibleFaces.VisibleFaceIndices;
        }

        var faceCount = graph.Triangles.Count;
        if (faceCount <= 0)
        {
            return [];
        }

        var faceVisible = graph.DefaultFaceIdVisibility?.FaceVisible;
        if (faceVisible is null || faceVisible.Length == 0)
        {
            return Enumerable.Range(0, faceCount).ToArray();
        }

        var visible = new List<int>(Math.Min(faceVisible.Length, faceCount));
        var limit = Math.Min(faceVisible.Length, faceCount);
        for (var face = 0; face < limit; face++)
        {
            if (faceVisible[face])
            {
                visible.Add(face);
            }
        }

        return visible.ToArray();
    }

    private static bool ShouldEmitToneRegion(ProjectedTriangle triangle)
    {
        return triangle.IsVisible &&
               triangle.IsFrontFacing &&
               triangle.ScreenArea > 0.05f &&
               !float.IsNaN(triangle.ScreenCenter.X) &&
               !float.IsNaN(triangle.ScreenCenter.Y) &&
               !float.IsNaN(triangle.ScreenArea) &&
               !float.IsInfinity(triangle.ScreenArea);
    }

    private static InteractiveToneBucket ClassifyBucket(float shade)
    {
        if (shade >= 0.66f)
        {
            return InteractiveToneBucket.Highlight;
        }

        if (shade >= 0.33f)
        {
            return InteractiveToneBucket.Midtone;
        }

        return InteractiveToneBucket.Shadow;
    }

    private static float ComputeCoverageOpacity(InteractiveToneBucket bucket, float shade)
    {
        return bucket switch
        {
            InteractiveToneBucket.Highlight => Math.Clamp(0.12f + (1f - shade) * 0.18f, 0.08f, 0.22f),
            InteractiveToneBucket.Midtone => Math.Clamp(0.24f + (0.66f - shade) * 0.20f, 0.20f, 0.34f),
            InteractiveToneBucket.Shadow => Math.Clamp(0.42f + (0.33f - shade) * 0.35f, 0.38f, 0.58f),
            _ => 0.25f
        };
    }

    private static int ResolveFaceStride(InteractiveQualityMode qualityMode)
    {
        return qualityMode switch
        {
            InteractiveQualityMode.FastPreview => 4,
            InteractiveQualityMode.BalancedViewport or InteractiveQualityMode.Auto => 2,
            InteractiveQualityMode.QualityViewport => 1,
            _ => 2
        };
    }

    private static int ResolveMaxRegions(InteractiveQualityMode qualityMode)
    {
        return qualityMode switch
        {
            InteractiveQualityMode.FastPreview => 512,
            InteractiveQualityMode.BalancedViewport or InteractiveQualityMode.Auto => 2_048,
            InteractiveQualityMode.QualityViewport => 8_192,
            _ => 2_048
        };
    }

    private static (int Highlight, int Midtone, int Shadow) CountBuckets(IReadOnlyList<InteractiveToneRegion> regions)
    {
        var highlight = 0;
        var midtone = 0;
        var shadow = 0;

        foreach (var region in regions)
        {
            switch (region.Bucket)
            {
                case InteractiveToneBucket.Highlight:
                    highlight++;
                    break;
                case InteractiveToneBucket.Midtone:
                    midtone++;
                    break;
                case InteractiveToneBucket.Shadow:
                    shadow++;
                    break;
            }
        }

        return (highlight, midtone, shadow);
    }
}
