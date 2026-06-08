namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed record InteractiveFrameBudget(
    TimeSpan Total,
    TimeSpan Cpu,
    TimeSpan GpuUpload,
    TimeSpan GpuDraw,
    TimeSpan Safety)
{
    public static InteractiveFrameBudget For60Fps() => new(
        TimeSpan.FromMilliseconds(16.6),
        TimeSpan.FromMilliseconds(6.0),
        TimeSpan.FromMilliseconds(3.0),
        TimeSpan.FromMilliseconds(5.0),
        TimeSpan.FromMilliseconds(2.6));
}
