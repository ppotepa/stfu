namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed class InteractiveFrameBenchmarkReport
{
    public static InteractiveFrameBenchmarkReport Empty { get; } = new(Array.Empty<InteractiveFrameBenchmarkSample>());

    public InteractiveFrameBenchmarkReport(IReadOnlyList<InteractiveFrameBenchmarkSample> samples)
    {
        Samples = samples ?? Array.Empty<InteractiveFrameBenchmarkSample>();
        SampleCount = Samples.Count;

        if (SampleCount == 0)
        {
            Status = InteractiveFrameBenchmarkStatus.Unknown;
            return;
        }

        AverageStageMs = Average(Samples, sample => sample.TotalStageMs);
        MaxStageMs = Max(Samples, sample => sample.TotalStageMs);
        AverageProjectionMs = Average(Samples, sample => sample.ProjectionMs);
        AverageVisibilityMs = Average(Samples, sample => sample.VisibilityMs);
        AverageCandidateMs = Average(Samples, sample => sample.CandidateMs);
        AverageStrokePlanMs = Average(Samples, sample => sample.StrokePlanMs);
        AverageTonePlanMs = Average(Samples, sample => sample.TonePlanMs);
        AverageCandidateEdges = Average(Samples, sample => sample.CandidateEdges);
        AverageStrokeCommands = Average(Samples, sample => sample.StrokeCommands);
        AverageVisibleStrokeSegments = Average(Samples, sample => sample.VisibleStrokeSegments);
        AverageHealthScore = Average(Samples, sample => sample.OutputHealthScore);
        ReturnedInteractiveFrames = Count(Samples, sample => sample.ReturnedInteractiveFrame);
        ReturnedReferenceFallbackFrames = Count(Samples, sample => sample.ReturnedReferenceFallback);
        SelfContainedProjectionFrames = Count(Samples, sample => sample.ProjectionBuiltSelfContained);
        ProjectedTriangleCandidateFrames = Count(Samples, sample => sample.CandidateEdgesBuiltFromProjectedTriangles);
        InteractiveReturnRatio = Ratio(ReturnedInteractiveFrames, SampleCount);
        ReferenceFallbackRatio = Ratio(ReturnedReferenceFallbackFrames, SampleCount);
        SelfContainedProjectionRatio = Ratio(SelfContainedProjectionFrames, SampleCount);
        ProjectedTriangleCandidateRatio = Ratio(ProjectedTriangleCandidateFrames, SampleCount);
        Status = InteractiveFrameBenchmarkThresholds.Default.Evaluate(this);
    }

    public IReadOnlyList<InteractiveFrameBenchmarkSample> Samples { get; }
    public int SampleCount { get; }
    public double AverageStageMs { get; }
    public double MaxStageMs { get; }
    public double AverageProjectionMs { get; }
    public double AverageVisibilityMs { get; }
    public double AverageCandidateMs { get; }
    public double AverageStrokePlanMs { get; }
    public double AverageTonePlanMs { get; }
    public double AverageCandidateEdges { get; }
    public double AverageStrokeCommands { get; }
    public double AverageVisibleStrokeSegments { get; }
    public double AverageHealthScore { get; }
    public int ReturnedInteractiveFrames { get; }
    public int ReturnedReferenceFallbackFrames { get; }
    public int SelfContainedProjectionFrames { get; }
    public int ProjectedTriangleCandidateFrames { get; }
    public double InteractiveReturnRatio { get; }
    public double ReferenceFallbackRatio { get; }
    public double SelfContainedProjectionRatio { get; }
    public double ProjectedTriangleCandidateRatio { get; }
    public InteractiveFrameBenchmarkStatus Status { get; }

    public InteractiveFrameBenchmarkReport WithThresholds(InteractiveFrameBenchmarkThresholds thresholds)
    {
        ArgumentNullException.ThrowIfNull(thresholds);

        return new InteractiveFrameBenchmarkReport(Samples)
        {
            StatusOverride = thresholds.Evaluate(this)
        };
    }

    private InteractiveFrameBenchmarkStatus? StatusOverride { get; init; }
    public InteractiveFrameBenchmarkStatus EffectiveStatus => StatusOverride ?? Status;

    private static double Average(IReadOnlyList<InteractiveFrameBenchmarkSample> samples, Func<InteractiveFrameBenchmarkSample, double> selector)
    {
        if (samples.Count == 0)
        {
            return 0d;
        }

        var total = 0d;
        for (var i = 0; i < samples.Count; i++)
        {
            total += selector(samples[i]);
        }

        return total / samples.Count;
    }

    private static double Max(IReadOnlyList<InteractiveFrameBenchmarkSample> samples, Func<InteractiveFrameBenchmarkSample, double> selector)
    {
        var max = 0d;
        for (var i = 0; i < samples.Count; i++)
        {
            max = Math.Max(max, selector(samples[i]));
        }

        return max;
    }

    private static int Count(IReadOnlyList<InteractiveFrameBenchmarkSample> samples, Func<InteractiveFrameBenchmarkSample, bool> predicate)
    {
        var count = 0;
        for (var i = 0; i < samples.Count; i++)
        {
            if (predicate(samples[i]))
            {
                count++;
            }
        }

        return count;
    }

    private static double Ratio(int count, int total)
    {
        return total <= 0 ? 0d : count / (double)total;
    }
}
