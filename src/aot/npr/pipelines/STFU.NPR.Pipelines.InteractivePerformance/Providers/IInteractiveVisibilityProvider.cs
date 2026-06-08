using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Core;

namespace STFU.NPR.Pipeline.InteractivePerformance.Providers;

public interface IInteractiveVisibilityProvider
{
    string Name { get; }

    VisibleFaceSetArtifact BuildVisibleFaces(InteractiveFrameContext context);
}
