using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Core;

namespace STFU.NPR.Pipeline.InteractivePerformance.Providers;

public sealed class CpuApproxVisibilityProvider : IInteractiveVisibilityProvider
{
    public string Name => "CpuApproxVisibility";

    public VisibleFaceSetArtifact BuildVisibleFaces(InteractiveFrameContext context)
    {
        var key = new ArtifactKey(
            ArtifactKind.VisibleFaces,
            ContentHash: 0,
            CameraHash: 0,
            StyleHash: 0,
            Width: context.Intent.Width,
            Height: context.Intent.Height);

        return new VisibleFaceSetArtifact
        {
            Key = key,
            Revision = context.Intent.FrameId,
            LastBuildTime = TimeSpan.Zero,
            FaceCount = 0,
            VisibleFaceCount = 0,
            VisibleFaceIndices = [],
            ProviderName = Name
        };
    }
}
