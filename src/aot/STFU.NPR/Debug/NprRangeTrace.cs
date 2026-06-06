namespace STFU.NPR.Debug;

public readonly record struct NprRangeTrace(
    string StepName,
    int RangeIndex,
    int StartInclusive,
    int EndExclusive,
    int ThreadId,
    long ElapsedTicks);
