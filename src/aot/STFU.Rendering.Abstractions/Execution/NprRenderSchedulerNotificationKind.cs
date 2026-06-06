namespace STFU.Rendering.Abstractions.Execution;

public enum NprRenderSchedulerNotificationKind
{
    RequestDroppedBeforeEnqueue,
    ResultDroppedAfterRender,
    RenderCancelled,
    RenderFailed,
    StopTimeout
}
