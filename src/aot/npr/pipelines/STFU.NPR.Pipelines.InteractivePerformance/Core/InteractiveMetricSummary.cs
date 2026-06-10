namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed record InteractiveMetricSummary
{
    public required string Name { get; init; }
    public InteractiveMetricUnit Unit { get; init; }
    public int Count { get; init; }
    public double Min { get; init; }
    public double Max { get; init; }
    public double Average { get; init; }
    public double P50 { get; init; }
    public double P95 { get; init; }
    public double Last { get; init; }

    public bool HasSamples => Count > 0;

    public double Range => Max - Min;
}
