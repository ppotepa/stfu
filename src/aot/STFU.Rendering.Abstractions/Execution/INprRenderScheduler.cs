using STFU.Rendering.Abstractions.Requests;

namespace STFU.Rendering.Abstractions.Execution;

public interface INprRenderScheduler : IDisposable
{
    event Action<NprRenderResult>? RenderCompleted;

    long LatestRequestedRevision { get; }

    long LatestCompletedRevision { get; }

    ValueTask EnqueueAsync(
        NprRenderRequest request,
        CancellationToken cancellationToken = default);
}
