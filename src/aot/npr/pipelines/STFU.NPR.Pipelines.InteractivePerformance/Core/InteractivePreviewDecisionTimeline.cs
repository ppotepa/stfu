using System.Collections.Generic;
using System.Linq;

namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed class InteractivePreviewDecisionTimeline
{
    private readonly List<InteractivePreviewDecisionRecord> _records = new();

    public IReadOnlyList<InteractivePreviewDecisionRecord> Records => _records;

    public int Count => _records.Count;

    public double AcceptedRatio => Count == 0 ? 0d : _records.Count(record => record.WasAccepted) / (double)Count;

    public double FallbackRatio => Count == 0 ? 0d : _records.Count(record => record.ReturnedReferenceFallback) / (double)Count;

    public InteractivePreviewDecisionTimeline Add(InteractivePreviewDecisionRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        _records.Add(record);
        return this;
    }

    public InteractiveEvidenceBag ToEvidence(InteractiveEvidenceBag? bag = null)
    {
        bag ??= new InteractiveEvidenceBag();
        bag.Add("preview.records", Count.ToString(System.Globalization.CultureInfo.InvariantCulture), InteractiveEvidenceKind.PreviewDecision);
        bag.Add("preview.acceptedRatio", AcceptedRatio.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture), InteractiveEvidenceKind.PreviewDecision);
        bag.Add("preview.fallbackRatio", FallbackRatio.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture), InteractiveEvidenceKind.PreviewDecision,
            FallbackRatio > 0.5d ? InteractiveEvidenceSeverity.Warning : InteractiveEvidenceSeverity.Info);
        return bag;
    }
}
