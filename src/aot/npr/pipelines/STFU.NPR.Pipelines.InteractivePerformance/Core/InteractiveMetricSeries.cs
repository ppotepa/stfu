namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed class InteractiveMetricSeries
{
    private readonly List<InteractiveMetricSample> _samples = [];

    public InteractiveMetricSeries(string name, InteractiveMetricUnit unit)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Metric name must not be empty.", nameof(name));
        }

        Name = name;
        Unit = unit;
    }

    public string Name { get; }
    public InteractiveMetricUnit Unit { get; }
    public IReadOnlyList<InteractiveMetricSample> Samples => _samples;

    public void Add(long frameId, double value)
    {
        var sample = new InteractiveMetricSample(frameId, Name, value, Unit);
        if (!sample.IsValid)
        {
            return;
        }

        _samples.Add(sample);
    }

    public InteractiveMetricSummary Summarize()
    {
        if (_samples.Count == 0)
        {
            return new InteractiveMetricSummary
            {
                Name = Name,
                Unit = Unit,
                Count = 0,
                Min = 0,
                Max = 0,
                Average = 0,
                P50 = 0,
                P95 = 0,
                Last = 0
            };
        }

        var values = _samples.Select(static sample => sample.Value).Order().ToArray();
        var average = values.Average();
        return new InteractiveMetricSummary
        {
            Name = Name,
            Unit = Unit,
            Count = values.Length,
            Min = values[0],
            Max = values[^1],
            Average = average,
            P50 = Percentile(values, 0.50),
            P95 = Percentile(values, 0.95),
            Last = _samples[^1].Value
        };
    }

    private static double Percentile(IReadOnlyList<double> orderedValues, double percentile)
    {
        if (orderedValues.Count == 0)
        {
            return 0;
        }

        if (orderedValues.Count == 1)
        {
            return orderedValues[0];
        }

        var index = Math.Clamp(percentile, 0, 1) * (orderedValues.Count - 1);
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);
        if (lower == upper)
        {
            return orderedValues[lower];
        }

        var fraction = index - lower;
        return orderedValues[lower] + ((orderedValues[upper] - orderedValues[lower]) * fraction);
    }
}
