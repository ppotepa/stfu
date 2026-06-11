using System.Collections.Generic;
using System.Linq;

namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed class InteractiveStageTimingTimeline
{
    private readonly List<InteractiveStageTimingRecord> _records = new();

    public IReadOnlyList<InteractiveStageTimingRecord> Records => _records;

    public InteractiveStageTimingTimeline Add(InteractiveStageTimingRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        _records.Add(record);
        return this;
    }

    public double TotalMs => _records.Sum(record => record.Milliseconds);

    public double MaxStageMs => _records.Count == 0 ? 0d : _records.Max(record => record.Milliseconds);

    public string? SlowestStage => _records.Count == 0
        ? null
        : _records.OrderByDescending(record => record.Milliseconds).First().Stage;

    public IReadOnlyDictionary<string, double> TotalsByStage()
    {
        return _records
            .GroupBy(record => record.Stage, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Sum(record => record.Milliseconds), StringComparer.Ordinal);
    }
}
