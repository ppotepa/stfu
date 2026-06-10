using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Core;

namespace STFU.NPR.Pipeline.InteractivePerformance.Providers;

public sealed class CpuReferenceVisibilityProvider : IInteractiveVisibilityProvider
{
    public string Name => "CpuReferenceVisibility";

    public VisibleFaceSetArtifact BuildVisibleFaces(InteractiveFrameContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var graph = context.ReferenceContext.Graph;
        var faceCount = graph.Triangles.Count;
        var visibleFaces = ExtractVisibleFaces(graph.DefaultFaceIdVisibility?.FaceVisible, faceCount);

        var key = ArtifactKeyFactory.VisibleFaces(context.Intent, faceCount);

        return new VisibleFaceSetArtifact
        {
            Key = key,
            Revision = context.Intent.FrameId,
            LastBuildTime = TimeSpan.Zero,
            FaceCount = faceCount,
            VisibleFaceCount = visibleFaces.Length,
            VisibleFaceIndices = visibleFaces,
            ProviderName = Name
        };
    }

    private static int[] ExtractVisibleFaces(bool[]? faceVisible, int faceCount)
    {
        if (faceCount <= 0)
        {
            return [];
        }

        if (faceVisible is null || faceVisible.Length == 0)
        {
            return Enumerable.Range(0, faceCount).ToArray();
        }

        var limit = Math.Min(faceVisible.Length, faceCount);
        var visible = new List<int>(limit);
        for (var face = 0; face < limit; face++)
        {
            if (faceVisible[face])
            {
                visible.Add(face);
            }
        }

        return visible.ToArray();
    }
}
