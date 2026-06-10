using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.NPR.Pipelines.Abstractions;
using STFU.Strokes;
using Xunit;

namespace STFU.NPR.Pipelines.Tests;

public sealed class InteractivePreviewPolicyTests
{
    [Fact]
    public void Decide_uses_reference_fallback_when_force_fallback_is_enabled()
    {
        var result = CreateResult(CreateRenderableFrameArtifact());
        var options = FramePipelineStrategyOptions.Default with
        {
            ForceReferenceFallback = true,
            EnableInteractivePreviewOutput = true,
            UseReferenceFallbackForFinalFrame = false
        };

        var decision = InteractivePreviewPolicy.Decide(options, result);

        Assert.Equal(InteractivePreviewDecisionKind.ForcedReferenceFallback, decision.Kind);
        Assert.True(decision.UsesReferenceFallback);
        Assert.False(decision.SelectedInteractiveFrame);
        Assert.Null(decision.Frame);
    }

    [Fact]
    public void Decide_uses_reference_fallback_when_safe_final_frame_fallback_is_enabled()
    {
        var result = CreateResult(CreateRenderableFrameArtifact());
        var options = FramePipelineStrategyOptions.Default with
        {
            EnableInteractivePreviewOutput = true,
            UseReferenceFallbackForFinalFrame = true
        };

        var decision = InteractivePreviewPolicy.Decide(options, result);

        Assert.Equal(InteractivePreviewDecisionKind.ReferenceFallbackRequired, decision.Kind);
        Assert.True(decision.UsesReferenceFallback);
        Assert.Contains("collecting preview artifacts", decision.Reason);
    }

    [Fact]
    public void Decide_uses_reference_fallback_when_preview_output_is_disabled()
    {
        var result = CreateResult(CreateRenderableFrameArtifact());
        var options = FramePipelineStrategyOptions.Default with
        {
            EnableInteractivePreviewOutput = false,
            UseReferenceFallbackForFinalFrame = false
        };

        var decision = InteractivePreviewPolicy.Decide(options, result);

        Assert.Equal(InteractivePreviewDecisionKind.PreviewOutputDisabled, decision.Kind);
        Assert.True(decision.UsesReferenceFallback);
    }

    [Fact]
    public void Decide_reports_missing_interactive_stroke_frame()
    {
        var result = CreateResult(frameArtifact: null);
        var options = FramePipelineStrategyOptions.Default with
        {
            EnableInteractivePreviewOutput = true,
            UseReferenceFallbackForFinalFrame = false
        };

        var decision = InteractivePreviewPolicy.Decide(options, result);

        Assert.Equal(InteractivePreviewDecisionKind.MissingInteractiveStrokeFrame, decision.Kind);
        Assert.True(decision.UsesReferenceFallback);
    }

    [Fact]
    public void Decide_reports_empty_interactive_stroke_frame()
    {
        var result = CreateResult(CreateEmptyFrameArtifact());
        var options = FramePipelineStrategyOptions.Default with
        {
            EnableInteractivePreviewOutput = true,
            UseReferenceFallbackForFinalFrame = false
        };

        var decision = InteractivePreviewPolicy.Decide(options, result);

        Assert.Equal(InteractivePreviewDecisionKind.EmptyInteractiveStrokeFrame, decision.Kind);
        Assert.True(decision.UsesReferenceFallback);
    }

    [Fact]
    public void Decide_reports_missing_required_tone_coverage()
    {
        var result = CreateResult(CreateRenderableFrameArtifact());
        var options = FramePipelineStrategyOptions.Default with
        {
            EnableInteractivePreviewOutput = true,
            UseReferenceFallbackForFinalFrame = false,
            RequireToneCoverageForInteractivePreview = true
        };

        var decision = InteractivePreviewPolicy.Decide(options, result);

        Assert.Equal(InteractivePreviewDecisionKind.MissingToneCoverage, decision.Kind);
        Assert.True(decision.UsesReferenceFallback);
    }

    [Fact]
    public void Decide_selects_interactive_frame_when_gates_allow_it()
    {
        var result = CreateResult(CreateRenderableFrameArtifact());
        var options = FramePipelineStrategyOptions.Default with
        {
            EnableInteractivePreviewOutput = true,
            UseReferenceFallbackForFinalFrame = false,
            RequireToneCoverageForInteractivePreview = false
        };

        var decision = InteractivePreviewPolicy.Decide(options, result);

        Assert.Equal(InteractivePreviewDecisionKind.SelectedInteractiveFrame, decision.Kind);
        Assert.True(decision.SelectedInteractiveFrame);
        Assert.False(decision.UsesReferenceFallback);
        Assert.NotNull(decision.Frame);
        Assert.Equal(1, decision.FramePathCount);
        Assert.Equal(1, decision.FrameSegmentCount);
    }

    [Fact]
    public void Decide_rejects_preview_when_readiness_score_is_below_gate()
    {
        var result = CreateResult(CreateRenderableFrameArtifact(), readinessScore: 70);
        var options = FramePipelineStrategyOptions.Default with
        {
            EnableInteractivePreviewOutput = true,
            UseReferenceFallbackForFinalFrame = false,
            InteractivePreviewMinReadinessScore = 85
        };

        var decision = InteractivePreviewPolicy.Decide(options, result);

        Assert.Equal(InteractivePreviewDecisionKind.OutputReadinessTooLow, decision.Kind);
        Assert.True(decision.UsesReferenceFallback);
        Assert.Contains("below required", decision.Reason);
    }

    [Fact]
    public void Decide_rejects_preview_when_segment_budget_is_exceeded()
    {
        var result = CreateResult(CreateRenderableFrameArtifact(segmentCount: 3));
        var options = FramePipelineStrategyOptions.Default with
        {
            EnableInteractivePreviewOutput = true,
            UseReferenceFallbackForFinalFrame = false,
            InteractivePreviewMaxStrokeSegments = 2
        };

        var decision = InteractivePreviewPolicy.Decide(options, result);

        Assert.Equal(InteractivePreviewDecisionKind.StrokeSegmentBudgetExceeded, decision.Kind);
        Assert.True(decision.UsesReferenceFallback);
        Assert.Contains("above preview budget", decision.Reason);
    }

    [Fact]
    public void TrySelectInteractiveFrame_preserves_legacy_boolean_contract()
    {
        var result = CreateResult(CreateRenderableFrameArtifact());
        var options = FramePipelineStrategyOptions.Default with
        {
            EnableInteractivePreviewOutput = true,
            UseReferenceFallbackForFinalFrame = false
        };

        var selected = InteractivePreviewPolicy.TrySelectInteractiveFrame(
            options,
            result,
            out var frame,
            out var reason);

        Assert.True(selected);
        Assert.NotSame(StrokeFrame.Empty, frame);
        Assert.Contains("selected", reason);
    }

    private static InteractivePipelineResult CreateResult(
        InteractiveStrokeFrameArtifact? frameArtifact,
        ToneCoverageArtifact? toneCoverage = null,
        int? readinessScore = null)
    {
        var output = new InteractiveOutputSelection
        {
            Summary = new InteractiveOutputSummary
            {
                Kind = frameArtifact?.HasRenderableFrame == true
                    ? InteractiveOutputKind.InteractiveStrokeFrame
                    : InteractiveOutputKind.ReferenceFallback,
                Readiness = frameArtifact?.HasRenderableFrame == true
                    ? InteractiveOutputReadiness.StrokeFrameReady
                    : InteractiveOutputReadiness.None,
                ReadinessScore = readinessScore ?? (frameArtifact?.HasRenderableFrame == true ? 85 : 0),
                HasInteractiveStrokeFrame = frameArtifact is not null,
                InteractiveStrokeFramePathCount = frameArtifact?.PathCount ?? 0,
                InteractiveStrokeFrameSegmentCount = frameArtifact?.FrameSegmentCount ?? 0,
                HasToneCoverage = toneCoverage is not null,
                ToneRegionCount = toneCoverage?.RegionCount ?? 0,
                Reason = "test output"
            },
            InteractiveStrokeFrame = frameArtifact,
            ToneCoverage = toneCoverage
        };

        return new InteractivePipelineResult(new InteractiveFrameDiagnostics(), output);
    }

    private static InteractiveStrokeFrameArtifact CreateRenderableFrameArtifact(int segmentCount = 1)
    {
        var segments = Enumerable.Range(0, Math.Max(1, segmentCount))
            .Select(index => new StrokeSegment2D(
                new Point2D(1 + index, 1),
                new Point2D(20 + index, 20),
                StrokeStyle2D.Default,
                new StrokeMetadata(
                    StableId: index + 1,
                    Layer: "interactive",
                    SourceKind: "test")))
            .ToArray();
        var frame = new StrokeFrame(
            64,
            64,
            new StrokeSegmentPathList(segments),
            segments);

        return CreateFrameArtifact(frame, sourceSegmentCount: segments.Length);
    }

    private static InteractiveStrokeFrameArtifact CreateEmptyFrameArtifact()
    {
        return CreateFrameArtifact(new StrokeFrame(64, 64, [], []), sourceSegmentCount: 0);
    }

    private static InteractiveStrokeFrameArtifact CreateFrameArtifact(
        StrokeFrame frame,
        int sourceSegmentCount)
    {
        return new InteractiveStrokeFrameArtifact
        {
            Key = new ArtifactKey(ArtifactKind.InteractiveStrokeFrame, 1, 1, 1, frame.Width, frame.Height),
            Revision = 1,
            LastBuildTime = TimeSpan.Zero,
            SourceSegmentCount = sourceSegmentCount,
            Frame = frame
        };
    }
}
