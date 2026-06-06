using System.Collections.Concurrent;
using STFU.Camera;
using STFU.NPR.Settings;
using STFU.Rendering.Abstractions.Diagnostics;
using STFU.Rendering.Abstractions.Execution;
using STFU.Rendering.Abstractions.Requests;
using Xunit;

namespace STFU.Rendering.Abstractions.Tests;

public sealed class LatestNprRenderSchedulerTests
{
    [Fact]
    public async Task Enqueue_DoesNotCancelInFlightRender_WhenNewerRequestArrives()
    {
        using var firstStarted = new ManualResetEventSlim();
        using var secondEnqueued = new ManualResetEventSlim();
        using var checkedFirstToken = new ManualResetEventSlim();
        using var releaseFirst = new ManualResetEventSlim();
        using var secondCompleted = new ManualResetEventSlim();
        var results = new ConcurrentQueue<NprRenderResult>();
        var firstTokenCancelled = true;

        using var scheduler = new LatestNprRenderScheduler(
            "test latest scheduler",
            (request, token) =>
            {
                if (request.Revision == 1)
                {
                    firstStarted.Set();
                    Assert.True(secondEnqueued.Wait(TimeSpan.FromSeconds(2)));
                    firstTokenCancelled = token.IsCancellationRequested;
                    checkedFirstToken.Set();
                    Assert.True(releaseFirst.Wait(TimeSpan.FromSeconds(2)));
                }

                return ValueTask.FromResult(CreateResult(request.Revision));
            });

        scheduler.RenderCompleted += result =>
        {
            results.Enqueue(result);
            if (result.Revision == 2 && result.Status == NprRenderStatus.Completed)
            {
                secondCompleted.Set();
            }
        };

        await scheduler.EnqueueAsync(CreateRequest(1));
        Assert.True(firstStarted.Wait(TimeSpan.FromSeconds(2)));

        await scheduler.EnqueueAsync(CreateRequest(2));
        secondEnqueued.Set();

        Assert.True(checkedFirstToken.Wait(TimeSpan.FromSeconds(2)));
        Assert.False(firstTokenCancelled);

        releaseFirst.Set();
        Assert.True(secondCompleted.Wait(TimeSpan.FromSeconds(2)));
        Assert.Contains(results, result => result.Revision == 1 && result.Status == NprRenderStatus.Dropped);
        Assert.Contains(results, result => result.Revision == 2 && result.Status == NprRenderStatus.Completed);

        while (results.TryDequeue(out var result))
        {
            result.Dispose();
        }
    }

    private static NprRenderRequest CreateRequest(long revision)
    {
        return new NprRenderRequest(
            Revision: revision,
            Width: 16,
            Height: 16,
            ExecutionProfile: NprExecutionProfile.FullCpuReference,
            ContentKind: NprRenderContentKind.NprPipeline,
            Scene: null!,
            Assets: null!,
            Camera: CameraState.Default,
            Settings: new NprSettings(),
            Style: null!,
            StyleSet: null!,
            EntityStyles: null!,
            Analysis: null!,
            FrameHistoryState: null!,
            Pipeline: null,
            ActivePresetId: "test",
            ActivePipelineId: "test",
            FrameId: (int)revision,
            TimeSeconds: 0f,
            PreviousFrame: null,
            Quality: NprQualityProfile.Default,
            Budget: new NprFrameBudget(AllowDroppingOldFrames: true),
            Theme: NprRenderTheme.Light,
            ShowGrid: false);
    }

    private static NprRenderResult CreateResult(long revision)
    {
        return new NprRenderResult
        {
            Revision = revision,
            Status = NprRenderStatus.Completed,
            ExecutionProfile = NprExecutionProfile.FullCpuReference,
            OutputKind = NprRenderOutputKind.None,
            Diagnostics = new NprRenderDiagnostics()
        };
    }
}
