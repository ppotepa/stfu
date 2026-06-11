namespace STFU.Rendering.Abstractions.Visibility;

public interface IVisibilityBufferProvider
{
    bool IsAvailable { get; }

    VisibilityBufferResult BuildVisibility(
        VisibilityBufferRequest request,
        CancellationToken cancellationToken);
}

public sealed record VisibilityBufferRequest(
    int Width,
    int Height,
    int FaceCount,
    int WorkerCount,
    bool PreferGpu,
    bool RequireReadback);

public sealed record VisibilityBufferResult(
    bool UsedGpu,
    bool UsedFallback,
    int Width,
    int Height,
    int VisibleFaceCount,
    IReadOnlyList<int> VisibleFaceIds);
