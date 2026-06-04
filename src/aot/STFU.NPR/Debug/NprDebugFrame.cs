namespace STFU.NPR.Debug;

public sealed record NprDebugFrame(
    IReadOnlyList<NprDebugLine> Lines,
    NprDebugCounters Counters,
    IReadOnlyList<NprStepTrace> StepTraces)
{
    public static NprDebugFrame Empty { get; } = new(
        [],
        new NprDebugCounters(0, 0, 0, 0, 0, 0, 0, 0, 0),
        []);
}
