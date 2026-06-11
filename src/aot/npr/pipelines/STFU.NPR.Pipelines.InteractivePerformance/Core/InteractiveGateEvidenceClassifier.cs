namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public static class InteractiveGateEvidenceClassifier
{
    public static InteractiveEvidenceSeverity FromStatus(InteractivePerformanceGateStatus status)
    {
        return status switch
        {
            InteractivePerformanceGateStatus.Pass => InteractiveEvidenceSeverity.Info,
            InteractivePerformanceGateStatus.Warning => InteractiveEvidenceSeverity.Warning,
            InteractivePerformanceGateStatus.Fail => InteractiveEvidenceSeverity.Failure,
            _ => InteractiveEvidenceSeverity.Warning
        };
    }

    public static InteractiveEvidenceBag AddGateResult(this InteractiveEvidenceBag bag, InteractivePerformanceGateResult result)
    {
        ArgumentNullException.ThrowIfNull(bag);
        ArgumentNullException.ThrowIfNull(result);

        var severity = FromStatus(result.Status);
        bag.Add("gate.status", result.Status.ToString(), InteractiveEvidenceKind.Gate, severity);
        bag.Add("gate.speedupRatio", result.SpeedupRatio.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture), InteractiveEvidenceKind.Gate, severity);
        bag.Add("gate.referenceFallbackRatio", result.ReferenceFallbackRatio.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture), InteractiveEvidenceKind.Gate, severity);
        return bag;
    }
}
