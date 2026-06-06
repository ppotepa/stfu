namespace STFU.Rendering.Abstractions.Execution;

public readonly record struct NprRenderSchedulerNotification(
    NprRenderSchedulerNotificationKind Kind,
    string SchedulerName,
    long Revision,
    long LatestRequestedRevision,
    NprExecutionProfile ExecutionProfile,
    int Width,
    int Height,
    NprRenderStatus? Status = null,
    Exception? Exception = null);
