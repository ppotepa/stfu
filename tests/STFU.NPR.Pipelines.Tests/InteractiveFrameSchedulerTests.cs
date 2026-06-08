using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;
using STFU.NPR.Pipelines.Abstractions;
using Xunit;

namespace STFU.NPR.Pipelines.Tests;

public sealed class InteractiveFrameSchedulerTests
{
    [Fact]
    public void InteractiveFrameScheduler_uses_visibility_refresh_when_camera_moves()
    {
        var scheduler = new InteractiveFrameScheduler();

        var work = scheduler.SelectWork(
            new InteractiveFrameIntent(
                1,
                1280,
                720,
                FramePipelineStrategy.InteractivePerformance,
                InteractiveQualityMode.Auto,
                TimeSpan.FromMilliseconds(16.6),
                CameraChanged: true,
                SceneChanged: false,
                AnimationChanged: false,
                StyleChanged: false,
                ViewportSizeChanged: false,
                DebugOverlayChanged: false),
            new InteractiveFrameDiagnostics());

        Assert.Equal(InteractiveWorkClass.VisibilityRefresh, work);
    }

    [Fact]
    public void InteractiveFrameScheduler_uses_fallback_on_debug_overlay()
    {
        var scheduler = new InteractiveFrameScheduler();

        var work = scheduler.SelectWork(
            new InteractiveFrameIntent(
                1,
                1280,
                720,
                FramePipelineStrategy.InteractivePerformance,
                InteractiveQualityMode.Auto,
                TimeSpan.FromMilliseconds(16.6),
                CameraChanged: false,
                SceneChanged: false,
                AnimationChanged: false,
                StyleChanged: false,
                ViewportSizeChanged: false,
                DebugOverlayChanged: true),
            new InteractiveFrameDiagnostics());

        Assert.Equal(InteractiveWorkClass.ReferenceFallback, work);
    }
}
