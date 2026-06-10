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
        ToneCoverageArtifact? toneCoverage = null)
    {
        var output = new InteractiveOutputSelection
        {
            Summary = new InteractiveOutputSummary
            {
                Kind = frameArtifact?.HasRenderableFrame == true
                    ? InteractiveOutputKind.InteractiveStrokeFrame
                    : InteractiveOutputKind.ReferenceFallback,
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

    private static InteractiveStrokeFrameArtifact CreateRenderableFrameArtifact()
    {
        var segment = new StrokeSegment2D(
            new Point2D(1, 1),
            new Point2D(20, 20),
            StrokeStyle2D.Default,
            new StrokeMetadata(
                StableId: 1,
                Layer: "interactive",
                SourceKind: "test"));
        var segments = new[] { segment };
        var frame = new StrokeFrame(
            64,
            64,
            new StrokeSegmentPathList(segments),
            segments);

        return CreateFrameArtifact(frame, sourceSegmentCount: 1);
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
