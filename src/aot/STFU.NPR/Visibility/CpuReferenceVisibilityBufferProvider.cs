using STFU.Rendering.Abstractions.Visibility;

namespace STFU.NPR.Visibility;

public sealed class CpuReferenceVisibilityBufferProvider : IVisibilityBufferProvider
{
    public bool IsAvailable => true;

    public VisibilityBufferResult BuildVisibility(VisibilityBufferRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return new VisibilityBufferResult(
            UsedGpu: false,
            UsedFallback: true,
            request.Width,
            request.Height,
            0,
            Array.Empty<int>());
    }
}
