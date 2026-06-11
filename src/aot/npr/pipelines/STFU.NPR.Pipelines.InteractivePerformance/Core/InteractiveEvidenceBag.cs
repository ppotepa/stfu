using System.Collections.Generic;
using System.Linq;

namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed class InteractiveEvidenceBag
{
    private readonly List<InteractiveEvidenceFact> _facts = new();

    public IReadOnlyList<InteractiveEvidenceFact> Facts => _facts;

    public int Count => _facts.Count;

    public bool HasFailures => _facts.Any(fact => fact.IsFailure);

    public bool HasWarnings => _facts.Any(fact => fact.IsWarning);

    public InteractiveEvidenceBag Add(
        string name,
        string value,
        InteractiveEvidenceKind kind = InteractiveEvidenceKind.Unknown,
        InteractiveEvidenceSeverity severity = InteractiveEvidenceSeverity.Info)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _facts.Add(new InteractiveEvidenceFact(name, value, kind, severity));
        return this;
    }

    public IEnumerable<InteractiveEvidenceFact> ByKind(InteractiveEvidenceKind kind)
    {
        return _facts.Where(fact => fact.Kind == kind);
    }

    public IEnumerable<InteractiveEvidenceFact> BySeverity(InteractiveEvidenceSeverity severity)
    {
        return _facts.Where(fact => fact.Severity == severity);
    }
}
