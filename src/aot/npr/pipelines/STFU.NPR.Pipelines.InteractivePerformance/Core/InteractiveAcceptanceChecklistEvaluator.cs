namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public static class InteractiveAcceptanceChecklistEvaluator
{
    public static InteractiveAcceptanceChecklist BuildDefault(
        InteractiveRunComparisonSnapshot comparison,
        InteractiveEvidenceReport evidence)
    {
        ArgumentNullException.ThrowIfNull(comparison);
        ArgumentNullException.ThrowIfNull(evidence);

        return new InteractiveAcceptanceChecklist(new[]
        {
            new InteractiveAcceptanceChecklistItem(
                "speedup",
                "Interactive Performance is faster than Reference Quality",
                comparison.SpeedupRatio > 1d,
                InteractiveEvidenceSeverity.Failure),
            new InteractiveAcceptanceChecklistItem(
                "interactive-return",
                "Interactive viewport path returns preview frames",
                comparison.InteractiveReturnRatio > 0d,
                InteractiveEvidenceSeverity.Warning),
            new InteractiveAcceptanceChecklistItem(
                "fallback-budget",
                "Reference fallback ratio stays bounded",
                comparison.FallbackRatio <= 0.25d,
                InteractiveEvidenceSeverity.Warning),
            new InteractiveAcceptanceChecklistItem(
                "evidence",
                "Evidence report has no failures",
                evidence.FailureCount == 0,
                InteractiveEvidenceSeverity.Failure)
        });
    }

    public static InteractiveEvidenceBag ToEvidence(this InteractiveAcceptanceChecklist checklist, InteractiveEvidenceBag? bag = null)
    {
        ArgumentNullException.ThrowIfNull(checklist);
        bag ??= new InteractiveEvidenceBag();

        bag.Add("checklist.passed", checklist.Passed.ToString(), InteractiveEvidenceKind.Checklist,
            checklist.Passed ? InteractiveEvidenceSeverity.Info : InteractiveEvidenceSeverity.Failure);
        bag.Add("checklist.failedCount", checklist.FailedCount.ToString(System.Globalization.CultureInfo.InvariantCulture), InteractiveEvidenceKind.Checklist,
            checklist.FailedCount > 0 ? InteractiveEvidenceSeverity.Failure : InteractiveEvidenceSeverity.Info);

        return bag;
    }
}
