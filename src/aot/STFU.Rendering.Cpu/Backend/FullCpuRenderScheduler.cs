using STFU.Rendering.Abstractions.Execution;
using STFU.Rendering.Abstractions.Requests;

namespace STFU.Rendering.Cpu.Backend;

public sealed class FullCpuRenderScheduler : INprRenderScheduler
{
    private readonly LatestNprRenderScheduler _scheduler;

    public FullCpuRenderScheduler(FullCpuRenderWorker worker)
    {
        ArgumentNullException.ThrowIfNull(worker);

        _scheduler = new LatestNprRenderScheduler(
            "STFU Full CPU NPR Render Worker",
            worker.RenderAsync);
        _scheduler.RenderCompleted += OnRenderCompleted;
    }

    public event Action<NprRenderResult>? RenderCompleted;

    public long LatestRequestedRevision => _scheduler.LatestRequestedRevision;

    public long LatestCompletedRevision => _scheduler.LatestCompletedRevision;

    public ValueTask EnqueueAsync(
        NprRenderRequest request,
        CancellationToken cancellationToken = default)
    {
        return _scheduler.EnqueueAsync(request, cancellationToken);
    }

    private void OnRenderCompleted(NprRenderResult result)
    {
        RenderCompleted?.Invoke(result);
    }

    public void Dispose()
    {
        _scheduler.RenderCompleted -= OnRenderCompleted;
        _scheduler.Dispose();
    }
}
