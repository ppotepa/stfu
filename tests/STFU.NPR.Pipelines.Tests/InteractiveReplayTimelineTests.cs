using STFU.NPR.Pipeline.InteractivePerformance.Core;
using Xunit;

namespace STFU.NPR.Pipelines.Tests;

public sealed class InteractiveReplayTimelineTests
{
    [Fact]
    public void Preview_decision_timeline_calculates_ratios()
    {
        var timeline = new InteractivePreviewDecisionTimeline()
            .Add(new InteractivePreviewDecisionRecord(1, InteractivePreviewDecisionKind.PreviewReady, 1, 10, true, false))
            .Add(new InteractivePreviewDecisionRecord(2, InteractivePreviewDecisionKind.OutputReadinessTooLow, 0.2, 0, false, true));

        Assert.Equal(2, timeline.Count);
        Assert.Equal(0.5, timeline.AcceptedRatio);
        Assert.Equal(0.5, timeline.FallbackRatio);
    }

    [Fact]
    public void Stage_timing_timeline_finds_slowest_stage()
    {
        var timeline = new InteractiveStageTimingTimeline()
            .Add(new InteractiveStageTimingRecord(1, "projection", 2))
            .Add(new InteractiveStageTimingRecord(1, "candidate", 7))
            .Add(new InteractiveStageTimingRecord(1, "stroke", 4));

        Assert.Equal(13, timeline.TotalMs);
        Assert.Equal("candidate", timeline.SlowestStage);
        Assert.True(timeline.TotalsByStage().ContainsKey("projection"));
    }

    [Fact]
    public void Replay_simulator_returns_interactive_when_frame_is_ready()
    {
        var input = new InteractiveFrameReplayInput(
            FrameId: 1,
            ProjectionMs: 1,
            VisibilityMs: 1,
            CandidateMs: 1,
            StrokePlanMs: 1,
            TonePlanMs: 1,
            HasPreviewCandidate: true,
            HasToneCoverage: true,
            VisibleStrokeSegments: 100);

        var result = InteractiveFrameReplaySimulator.Replay(input, maxStageMs: 16.67, maxVisibleStrokeSegments: 1000);

        Assert.True(result.ReturnedInteractiveFrame);
        Assert.False(result.ReturnedReferenceFallback);
        Assert.Equal(InteractivePreviewDecisionKind.PreviewReady, result.PreviewDecision);
    }

    [Fact]
    public void Replay_simulator_rejects_over_budget_segments()
    {
        var input = new InteractiveFrameReplayInput(
            FrameId: 1,
            ProjectionMs: 1,
            VisibilityMs: 1,
            CandidateMs: 1,
            StrokePlanMs: 1,
            TonePlanMs: 1,
            HasPreviewCandidate: true,
            HasToneCoverage: true,
            VisibleStrokeSegments: 5000);

        var result = InteractiveFrameReplaySimulator.Replay(input, maxStageMs: 16.67, maxVisibleStrokeSegments: 1000);

        Assert.False(result.ReturnedInteractiveFrame);
        Assert.True(result.ReturnedReferenceFallback);
        Assert.Equal(InteractivePreviewDecisionKind.StrokeSegmentBudgetExceeded, result.PreviewDecision);
    }
}
