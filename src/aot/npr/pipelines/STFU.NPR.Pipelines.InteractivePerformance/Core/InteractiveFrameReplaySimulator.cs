namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public static class InteractiveFrameReplaySimulator
{
    public static InteractiveFrameReplayResult Replay(
        InteractiveFrameReplayInput input,
        double maxStageMs = 16.67d,
        int maxVisibleStrokeSegments = 65536)
    {
        ArgumentNullException.ThrowIfNull(input);

        var returnedInteractive = input.HasPreviewCandidate
            && input.HasToneCoverage
            && input.VisibleStrokeSegments <= maxVisibleStrokeSegments
            && input.TotalStageMs <= maxStageMs;

        var decision = returnedInteractive
            ? InteractivePreviewDecisionKind.SelectedInteractiveFrame
            : input.VisibleStrokeSegments > maxVisibleStrokeSegments
                ? InteractivePreviewDecisionKind.StrokeSegmentBudgetExceeded
                : InteractivePreviewDecisionKind.OutputReadinessTooLow;

        var bag = new InteractiveEvidenceBag()
            .Add("replay.frameId", input.FrameId.ToString(System.Globalization.CultureInfo.InvariantCulture), InteractiveEvidenceKind.Scenario)
            .Add("replay.totalStageMs", input.TotalStageMs.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture), InteractiveEvidenceKind.StageTiming,
                input.TotalStageMs > maxStageMs ? InteractiveEvidenceSeverity.Warning : InteractiveEvidenceSeverity.Info)
            .Add("replay.previewDecision", decision.ToString(), InteractiveEvidenceKind.PreviewDecision,
                returnedInteractive ? InteractiveEvidenceSeverity.Info : InteractiveEvidenceSeverity.Warning);

        return new InteractiveFrameReplayResult(
            input.FrameId,
            input.TotalStageMs,
            returnedInteractive,
            !returnedInteractive,
            decision,
            InteractiveEvidenceReporter.Build("Interactive frame replay evidence", bag));
    }
}
