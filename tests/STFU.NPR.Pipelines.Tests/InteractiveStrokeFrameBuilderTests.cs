using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;
using STFU.NPR.Pipeline.InteractivePerformance.Stages;
using STFU.NPR.Pipelines.Abstractions;
using STFU.Strokes;
using Xunit;

namespace STFU.NPR.Pipelines.Tests;

public sealed class InteractiveStrokeFrameBuilderTests
{
    [Fact]
    public void InteractiveStrokeFrameBuilder_maps_visible_segments_to_stroke_frame()
    {
        var visibleSegments = new[]
        {
            new InteractiveVisibleStrokeSegment(
                SourceEdgeId: 42,
                Role: 0,
                X0: 10,
                Y0: 12,
                X1: 120,
                Y1: 64,
                Visibility: 0.9f,
                Importance: 1.2f),
            new InteractiveVisibleStrokeSegment(
                SourceEdgeId: 43,
                Role: 2,
                X0: 15,
                Y0: 20,
                X1: 30,
                Y1: 75,
                Visibility: 0.7f,
                Importance: 0.8f)
        };

        var frame = InteractiveStrokeFrameBuilder.BuildFrame(
            visibleSegments,
            width: 320,
            height: 240,
            InteractiveQualityMode.QualityViewport);

        Assert.Equal(320, frame.Width);
        Assert.Equal(240, frame.Height);
        Assert.Equal(2, frame.Paths.Count);
        Assert.NotNull(frame.Segments);
        Assert.Equal(2, frame.Segments!.Count);
        Assert.Equal("Silhouette", frame.Segments[0].Metadata?.Intent);
        Assert.Equal("Feature", frame.Segments[1].Metadata?.Intent);
        Assert.Equal("InteractivePerformance", frame.Segments[0].Metadata?.SourceKind);
        Assert.True(frame.Segments[0].Style.Thickness > frame.Segments[1].Style.Thickness);
        Assert.True(frame.Paths[0].TryGetSegment(out var start, out var end));
        Assert.Equal(10, start.X);
        Assert.Equal(64, end.Y);
    }

    [Fact]
    public void InteractiveStrokeFrameBuilder_clamps_coordinates_to_viewport()
    {
        var visibleSegments = new[]
        {
            new InteractiveVisibleStrokeSegment(
                SourceEdgeId: 1,
                Role: 1,
                X0: -10,
                Y0: -12,
                X1: 999,
                Y1: 888,
                Visibility: 1,
                Importance: 1)
        };

        var frame = InteractiveStrokeFrameBuilder.BuildFrame(
            visibleSegments,
            width: 100,
            height: 80,
            InteractiveQualityMode.QualityViewport);

        var segment = Assert.Single(frame.Segments!);
        Assert.Equal(0, segment.Start.X);
        Assert.Equal(0, segment.Start.Y);
        Assert.Equal(99, segment.End.X);
        Assert.Equal(79, segment.End.Y);
    }

    [Fact]
    public void InteractiveStrokeFrameBuilder_respects_explicit_preview_budget()
    {
        var visibleSegments = Enumerable.Range(0, 20)
            .Select(index => new InteractiveVisibleStrokeSegment(
                SourceEdgeId: index,
                Role: index % 4,
                X0: index,
                Y0: index + 1,
                X1: index + 20,
                Y1: index + 21,
                Visibility: 1,
                Importance: 1))
            .ToArray();

        var frame = InteractiveStrokeFrameBuilder.BuildFrame(
            visibleSegments,
            width: 200,
            height: 200,
            InteractiveQualityMode.QualityViewport,
            new InteractiveStrokeFrameBuildOptions { MaxSegments = 5 });

        Assert.Equal(5, frame.Segments!.Count);
        Assert.Equal(5, frame.Paths.Count);
    }

    [Fact]
    public void InteractiveStrokeFrameBuilder_skips_degenerate_segments()
    {
        var visibleSegments = new[]
        {
            new InteractiveVisibleStrokeSegment(1, 0, 10, 10, 10, 10, 1, 1),
            new InteractiveVisibleStrokeSegment(2, 0, 20, 20, 50, 50, 1, 1)
        };

        var frame = InteractiveStrokeFrameBuilder.BuildFrame(
            visibleSegments,
            width: 100,
            height: 100,
            InteractiveQualityMode.QualityViewport);

        var segment = Assert.Single(frame.Segments!);
        Assert.Equal(2, segment.Metadata?.SourceFeatureId);
    }

    [Fact]
    public void OutputSelector_prefers_interactive_stroke_frame_over_raw_segments()
    {
        var store = new ArtifactStore();
        var key = new ArtifactKey(ArtifactKind.InteractiveStrokeFrame, 1, 2, 3, 100, 100);
        var frame = InteractiveStrokeFrameBuilder.BuildFrame(
            [new InteractiveVisibleStrokeSegment(2, 0, 0, 0, 40, 40, 1, 1)],
            width: 100,
            height: 100,
            InteractiveQualityMode.QualityViewport);

        store.Set(new InteractiveStrokeFrameArtifact
        {
            Key = key,
            Revision = 1,
            SourceSegmentCount = 1,
            Frame = frame,
            LastBuildTime = TimeSpan.Zero
        });

        var output = InteractiveOutputSelector.Select(store);

        Assert.Equal(InteractiveOutputKind.InteractiveStrokeFrame, output.Summary.Kind);
        Assert.True(output.Summary.HasInteractiveStrokeFrame);
        Assert.Equal(1, output.Summary.InteractiveStrokeFrameSegmentCount);
        Assert.Same(frame, output.InteractiveStrokeFrame?.Frame);
    }

    [Fact]
    public void PreviewPolicy_rejects_interactive_frame_when_default_safe_fallback_is_enabled()
    {
        var result = CreateResultWithFrame();

        var selected = InteractivePreviewPolicy.TrySelectInteractiveFrame(
            FramePipelineStrategyOptions.Default,
            result,
            out var frame,
            out var reason);

        Assert.False(selected);
        Assert.Equal(StrokeFrame.Empty, frame);
        Assert.Contains("UseReferenceFallbackForFinalFrame", reason);
    }

    [Fact]
    public void PreviewPolicy_accepts_interactive_frame_when_preview_output_is_enabled()
    {
        var result = CreateResultWithFrame();
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
        Assert.Equal(1, frame.Paths.Count);
        Assert.Contains("selected", reason, StringComparison.OrdinalIgnoreCase);
    }

    private static InteractivePipelineResult CreateResultWithFrame()
    {
        var frame = InteractiveStrokeFrameBuilder.BuildFrame(
            [new InteractiveVisibleStrokeSegment(7, 0, 0, 0, 50, 50, 1, 1)],
            width: 100,
            height: 100,
            InteractiveQualityMode.QualityViewport);
        var artifact = new InteractiveStrokeFrameArtifact
        {
            Key = new ArtifactKey(ArtifactKind.InteractiveStrokeFrame, 1, 2, 3, 100, 100),
            Revision = 1,
            SourceSegmentCount = 1,
            Frame = frame,
            LastBuildTime = TimeSpan.Zero
        };

        return new InteractivePipelineResult(
            new InteractiveFrameDiagnostics(),
            new InteractiveOutputSelection
            {
                InteractiveStrokeFrame = artifact,
                Summary = new InteractiveOutputSummary
                {
                    Kind = InteractiveOutputKind.InteractiveStrokeFrame,
                    HasInteractiveStrokeFrame = true,
                    InteractiveStrokeFramePathCount = frame.Paths.Count,
                    InteractiveStrokeFrameSegmentCount = frame.Segments?.Count ?? 0,
                    Reason = "test frame"
                }
            });
    }
}
