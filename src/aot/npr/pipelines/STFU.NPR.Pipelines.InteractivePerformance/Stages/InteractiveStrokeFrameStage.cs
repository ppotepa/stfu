using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.Strokes;

namespace STFU.NPR.Pipeline.InteractivePerformance.Stages;

public sealed class InteractiveStrokeFrameStage : IInteractivePipelineStage
{
    public string Name => "InteractiveStrokeFrame";

    public bool ShouldRun(InteractiveFrameContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Artifacts.TryGetLatest(
            ArtifactKind.VisibleStrokeSegments,
            out VisibleStrokeSegmentArtifact? visibleSegments) &&
            visibleSegments.SegmentCount > 0;
    }

    public void Execute(InteractiveFrameContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.Artifacts.TryGetLatest(
                ArtifactKind.VisibleStrokeSegments,
                out VisibleStrokeSegmentArtifact? visibleSegments))
        {
            WriteEmptyDiagnostics(context);
            return;
        }

        var options = new InteractiveStrokeFrameBuildOptions
        {
            MaxSegments = context.Intent.Options.InteractivePreviewMaxStrokeSegments
        };
        var frame = InteractiveStrokeFrameBuilder.BuildFrame(
            visibleSegments.Segments,
            context.Intent.Width,
            context.Intent.Height,
            context.Intent.QualityMode,
            options);

        var key = ArtifactKeyFactory.InteractiveStrokeFrame(
            context.Intent,
            visibleSegments.SegmentCount,
            visibleSegments.Revision);

        var artifact = new InteractiveStrokeFrameArtifact
        {
            Key = key,
            Revision = context.Intent.FrameId,
            LastBuildTime = TimeSpan.Zero,
            SourceSegmentCount = visibleSegments.SegmentCount,
            Frame = frame,
            Note = $"interactive stroke frame {frame.Paths.Count} path(s) from {visibleSegments.SegmentCount} visible segment(s)"
        };

        context.Artifacts.Set(artifact);
        context.Diagnostics.InteractiveStrokeFrameSourceSegments = artifact.SourceSegmentCount;
        context.Diagnostics.InteractiveStrokeFramePaths = artifact.PathCount;
        context.Diagnostics.InteractiveStrokeFrameSegments = artifact.FrameSegmentCount;
        context.Diagnostics.InteractiveStrokeFrameCoveragePercent = artifact.StrokeFrameCoveragePercent;
    }

    private static void WriteEmptyDiagnostics(InteractiveFrameContext context)
    {
        context.Diagnostics.InteractiveStrokeFrameSourceSegments = 0;
        context.Diagnostics.InteractiveStrokeFramePaths = 0;
        context.Diagnostics.InteractiveStrokeFrameSegments = 0;
        context.Diagnostics.InteractiveStrokeFrameCoveragePercent = 0d;
    }
}
