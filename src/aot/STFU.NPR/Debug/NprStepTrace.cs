namespace STFU.NPR.Debug;

public readonly record struct NprStepTrace(
    string StepName,
    double Milliseconds,
    int InputCount,
    int OutputCount,
    int RejectedCount,
    string Notes);
