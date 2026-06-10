using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Core;

namespace STFU.NPR.Pipeline.InteractivePerformance.Providers;

public sealed class CpuApproxVisibilityProvider : IInteractiveVisibilityProvider
{
    public string Name => "CpuApproxVisibility";

    public VisibleFaceSetArtifact BuildVisibleFaces(InteractiveFrameContext context)
    {
        var faceCount = context.ReferenceContext.Graph.Triangles.Count;
        var visibleFaces = faceCount <= 0 ? Array.Empty<int>() : Enumerable.Range(0, faceCount).ToArray();

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
}
