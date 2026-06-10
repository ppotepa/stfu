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

    [Fact]
    public void InteractiveFrameChangeTracker_detects_reuse_when_signature_is_unchanged()
    {
        var tracker = new InteractiveFrameChangeTracker();
        var scheduler = new InteractiveFrameScheduler();
        var intent = CreateIntent(new InteractiveFrameSignature(1, 2, 3, 4, 5));

        var first = tracker.Resolve(intent);
        var second = tracker.Resolve(intent);

        Assert.True(first.SceneChanged);
        Assert.True(first.CameraChanged);
        Assert.True(first.StyleChanged);
        Assert.False(second.SceneChanged);
        Assert.False(second.CameraChanged);
        Assert.False(second.StyleChanged);

        var work = scheduler.SelectWork(second, new InteractiveFrameDiagnostics());

        Assert.Equal(InteractiveWorkClass.ReuseOnly, work);
    }

    [Fact]
    public void InteractiveFrameChangeTracker_detects_camera_changes()
    {
        var tracker = new InteractiveFrameChangeTracker();
        _ = tracker.Resolve(CreateIntent(new InteractiveFrameSignature(1, 2, 3, 4, 5)));

        var changed = tracker.Resolve(CreateIntent(new InteractiveFrameSignature(1, 99, 3, 4, 5)));

        Assert.True(changed.CameraChanged);
        Assert.False(changed.SceneChanged);
        Assert.False(changed.StyleChanged);
    }


    [Fact]
    public void InteractiveFrameChangeTracker_treats_quality_change_as_style_change()
    {
        var tracker = new InteractiveFrameChangeTracker();
        _ = tracker.Resolve(CreateIntent(new InteractiveFrameSignature(1, 2, 3, 4, 5)));

        var changed = tracker.Resolve(
            CreateIntent(new InteractiveFrameSignature(1, 2, 3, 4, 5))
                with { QualityMode = InteractiveQualityMode.QualityViewport });

        Assert.True(changed.StyleChanged);
    }

    private static InteractiveFrameIntent CreateIntent(InteractiveFrameSignature signature)
    {
        return new InteractiveFrameIntent(
            1,
            1280,
            720,
            FramePipelineStrategy.InteractivePerformance,
            InteractiveQualityMode.BalancedViewport,
            TimeSpan.FromMilliseconds(16.6),
            CameraChanged: true,
            SceneChanged: true,
            AnimationChanged: false,
            StyleChanged: true,
            ViewportSizeChanged: false,
            DebugOverlayChanged: false,
            Signature: signature);
    }

}
