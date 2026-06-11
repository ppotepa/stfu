using System.Collections.Generic;
using System.Linq;

namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed class InteractiveEvidenceReport
{
    public InteractiveEvidenceReport(string title, IEnumerable<InteractiveEvidenceFact> facts)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(facts);

        Title = title;
        Facts = facts.ToArray();
        FailureCount = Facts.Count(fact => fact.IsFailure);
        WarningCount = Facts.Count(fact => fact.IsWarning);
        InfoCount = Facts.Count - FailureCount - WarningCount;
        Status = FailureCount > 0
            ? InteractiveEvidenceSeverity.Failure
            : WarningCount > 0
                ? InteractiveEvidenceSeverity.Warning
                : InteractiveEvidenceSeverity.Info;
    }

    public string Title { get; }

    public IReadOnlyList<InteractiveEvidenceFact> Facts { get; }

    public int InfoCount { get; }

    public int WarningCount { get; }

    public int FailureCount { get; }

    public InteractiveEvidenceSeverity Status { get; }

    public string StableHash => InteractiveEvidenceHash.Short(string.Join("|", Facts.Select(fact => $"{fact.Kind}:{fact.Severity}:{fact.Name}:{fact.Value}")));
}
