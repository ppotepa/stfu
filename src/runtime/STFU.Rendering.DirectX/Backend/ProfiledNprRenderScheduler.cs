using STFU.Logging;
using STFU.Rendering.Abstractions.Execution;
using STFU.Rendering.Abstractions.Requests;

namespace STFU.Rendering.DirectX.Backend;

public class ProfiledNprRenderScheduler : INprRenderScheduler
{
    private readonly LatestNprRenderScheduler _scheduler;

    public ProfiledNprRenderScheduler(DirectXRenderWorker worker)
    {
        ArgumentNullException.ThrowIfNull(worker);

        _scheduler = new LatestNprRenderScheduler(
            "STFU Profiled NPR Render Worker",
            worker.RenderAsync,
            LogSchedulerNotification);
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

    private static void LogSchedulerNotification(NprRenderSchedulerNotification notification)
    {
        switch (notification.Kind)
        {
            case NprRenderSchedulerNotificationKind.RequestDroppedBeforeEnqueue:
                StfuLog.Write(
                    StfuLogDomain.Viewport,
                    "request.dropped_before_enqueue",
                    $"revision={notification.Revision}",
                    StfuLogLevel.Debug,
                    CommonProperties(notification));
                break;
            case NprRenderSchedulerNotificationKind.ResultDroppedAfterRender:
                StfuLog.Write(
                    StfuLogDomain.Viewport,
                    "result.dropped",
                    $"revision={notification.Revision}",
                    StfuLogLevel.Debug,
                    CommonProperties(notification));
                break;
            case NprRenderSchedulerNotificationKind.RenderCancelled:
                StfuLog.Write(
                    StfuLogDomain.Viewport,
                    "render.cancelled",
                    $"revision={notification.Revision}",
                    StfuLogLevel.Debug,
                    CommonProperties(notification));
                break;
            case NprRenderSchedulerNotificationKind.RenderFailed:
                StfuLog.Write(
                    StfuLogDomain.Errors,
                    "render.failed",
                    notification.Exception?.Message ?? $"revision={notification.Revision}",
                    StfuLogLevel.Error,
                    CommonProperties(notification),
                    notification.Exception);
                break;
            case NprRenderSchedulerNotificationKind.StopTimeout:
                StfuLog.Write(
                    StfuLogDomain.Viewport,
                    "scheduler.stop_timeout",
                    $"{notification.SchedulerName} worker did not stop within timeout.",
                    StfuLogLevel.Warning,
                    CommonProperties(notification));
                break;
        }
    }

    private static Dictionary<string, object?> CommonProperties(NprRenderSchedulerNotification notification)
    {
        return new Dictionary<string, object?>
        {
            ["scheduler"] = notification.SchedulerName,
            ["revision"] = notification.Revision,
            ["latestRequested"] = notification.LatestRequestedRevision,
            ["profile"] = notification.ExecutionProfile,
            ["width"] = notification.Width,
            ["height"] = notification.Height,
            ["status"] = notification.Status
        };
    }

    public void Dispose()
    {
        _scheduler.RenderCompleted -= OnRenderCompleted;
        _scheduler.Dispose();
    }
}
