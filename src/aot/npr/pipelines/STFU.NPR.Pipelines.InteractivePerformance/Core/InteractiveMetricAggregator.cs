namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed class InteractiveMetricAggregator
{
    private readonly Dictionary<string, InteractiveMetricSeries> _series = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, InteractiveMetricSeries> Series => _series;

    public void Add(long frameId, string name, double value, InteractiveMetricUnit unit)
    {
        if (!_series.TryGetValue(name, out var series))
        {
            series = new InteractiveMetricSeries(name, unit);
            _series.Add(name, series);
        }

        series.Add(frameId, value);
    }

    public IReadOnlyList<InteractiveMetricSummary> Summarize()
    {
        return _series.Values
            .OrderBy(static series => series.Name, StringComparer.Ordinal)
            .Select(static series => series.Summarize())
            .ToArray();
    }

    public InteractiveMetricSummary Summarize(string name)
    {
        return _series.TryGetValue(name, out var series)
            ? series.Summarize()
            : new InteractiveMetricSummary
            {
                Name = name,
                Unit = InteractiveMetricUnit.Count,
                Count = 0,
                Min = 0,
                Max = 0,
                Average = 0,
                P50 = 0,
                P95 = 0,
                Last = 0
            };
    }
}
