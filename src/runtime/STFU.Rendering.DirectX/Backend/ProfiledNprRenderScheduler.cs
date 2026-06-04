using System.Diagnostics;
using STFU.Logging;
using STFU.Rendering.Abstractions.Diagnostics;
using STFU.Rendering.Abstractions.Execution;
using STFU.Rendering.Abstractions.Requests;

namespace STFU.Rendering.DirectX.Backend;

public class ProfiledNprRenderScheduler : INprRenderScheduler
{
    private readonly DirectXRenderWorker _worker;
    private readonly object _gate = new();
    private readonly AutoResetEvent _signal = new(false);
    private readonly Thread _thread;
    private readonly CancellationTokenSource _disposeCts = new();
    private CancellationTokenSource? _currentRenderCts;
    private NprRenderRequest? _latestRequest;
    private bool _disposed;

    public ProfiledNprRenderScheduler(DirectXRenderWorker worker)
    {
        _worker = worker;
        _thread = new Thread(WorkerLoop)
        {
            IsBackground = true,
            Name = "STFU Profiled NPR Render Worker"
        };
        _thread.Start();
    }

    public event Action<NprRenderResult>? RenderCompleted;

    public long LatestRequestedRevision { get; private set; }

    public long LatestCompletedRevision { get; private set; }

    public ValueTask EnqueueAsync(
        NprRenderRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_gate)
        {
            if (request.Revision <= LatestRequestedRevision && request.Budget.AllowDroppingOldFrames)
            {
                StfuLog.Write(
                    StfuLogDomain.Viewport,
                    "request.dropped_before_enqueue",
                    $"revision={request.Revision}",
                    StfuLogLevel.Debug,
                    new Dictionary<string, object?>
                    {
                        ["revision"] = request.Revision,
                        ["latestRequested"] = LatestRequestedRevision
                    });
                return ValueTask.CompletedTask;
            }

            LatestRequestedRevision = request.Revision;
            _latestRequest = request;
            _currentRenderCts?.Cancel();
        }

        _signal.Set();
        return ValueTask.CompletedTask;
    }

    private void WorkerLoop()
    {
        while (!_disposeCts.IsCancellationRequested)
        {
            _signal.WaitOne(16);
            if (_disposeCts.IsCancellationRequested)
            {
                break;
            }

            NprRenderRequest? request;
            CancellationTokenSource cts;
            lock (_gate)
            {
                request = _latestRequest;
                _latestRequest = null;
                _currentRenderCts?.Dispose();
                _currentRenderCts = CancellationTokenSource.CreateLinkedTokenSource(_disposeCts.Token);
                cts = _currentRenderCts;
            }

            if (request is null)
            {
                continue;
            }

            try
            {
                var result = _worker.RenderAsync(request, cts.Token).AsTask().GetAwaiter().GetResult();

                if (request.Budget.AllowDroppingOldFrames && result.Revision < LatestRequestedRevision)
                {
                    result.Dispose();
                    StfuLog.Write(
                        StfuLogDomain.Viewport,
                        "result.dropped",
                        $"revision={result.Revision}",
                        StfuLogLevel.Debug,
                        new Dictionary<string, object?>
                        {
                            ["revision"] = result.Revision,
                            ["latestRequested"] = LatestRequestedRevision
                        });
                    PublishDropped(request, NprRenderStatus.Dropped, null);
                    continue;
                }

                LatestCompletedRevision = result.Revision;
                RenderCompleted?.Invoke(result);
            }
            catch (OperationCanceledException)
            {
                PublishDropped(request, NprRenderStatus.Cancelled, null);
            }
            catch (Exception ex)
            {
                PublishDropped(request, NprRenderStatus.Failed, ex);
            }
        }
    }

    private void PublishDropped(NprRenderRequest request, NprRenderStatus status, Exception? exception)
    {
        var diagnostics = new NprRenderDiagnostics
        {
            Width = request.Width,
            Height = request.Height,
            Notes = exception?.Message
        };

        StfuLog.Write(
            status == NprRenderStatus.Failed ? StfuLogDomain.Errors : StfuLogDomain.Viewport,
            $"render.{status.ToString().ToLowerInvariant()}",
            exception?.Message ?? $"revision={request.Revision}",
            status == NprRenderStatus.Failed ? StfuLogLevel.Error : StfuLogLevel.Debug,
            new Dictionary<string, object?>
            {
                ["revision"] = request.Revision,
                ["profile"] = request.ExecutionProfile,
                ["width"] = request.Width,
                ["height"] = request.Height
            },
            exception);

        RenderCompleted?.Invoke(new NprRenderResult
        {
            Revision = request.Revision,
            Status = status,
            ExecutionProfile = request.ExecutionProfile,
            OutputKind = NprRenderOutputKind.None,
            Exception = exception,
            Diagnostics = diagnostics
        });
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _disposeCts.Cancel();
        _signal.Set();
        if (!_thread.Join(TimeSpan.FromSeconds(1)))
        {
            Debug.WriteLine("ProfiledNprRenderScheduler worker did not stop within timeout.");
            StfuLog.Write(
                StfuLogDomain.Viewport,
                "scheduler.stop_timeout",
                "ProfiledNprRenderScheduler worker did not stop within timeout.",
                StfuLogLevel.Warning);
        }

        _currentRenderCts?.Dispose();
        _disposeCts.Dispose();
        _signal.Dispose();
    }
}
