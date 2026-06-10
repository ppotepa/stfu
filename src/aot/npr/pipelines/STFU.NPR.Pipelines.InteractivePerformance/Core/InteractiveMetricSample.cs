namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public readonly record struct InteractiveMetricSample(
    long FrameId,
    string Name,
    double Value,
    InteractiveMetricUnit Unit)
{
    public bool IsValid => FrameId >= 0 && !string.IsNullOrWhiteSpace(Name) && !double.IsNaN(Value) && !double.IsInfinity(Value);
}
