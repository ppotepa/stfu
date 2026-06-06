using System.Diagnostics;
using STFU.Rendering.Abstractions.Diagnostics;
using STFU.Common.Math;
using STFU.Rendering.Abstractions.Requests;

namespace STFU.Rendering.Abstractions.Execution;

public sealed class LatestNprRenderScheduler : INprRenderScheduler
{
    private readonly Func<NprRenderRequest, CancellationToken, ValueTask<NprRenderResult>> _renderAsync;
    private readonly Action<NprRenderSchedulerNotification>? _notify;
    private readonly object _gate = new();
    private readonly AutoResetEvent _signal = new(false);
    private readonly Thread _thread;
    private readonly CancellationTokenSource _disposeCts = new();
    private CancellationTokenSource? _currentRenderCts;
    private NprRenderRequest? _latestRequest;
    private bool _disposed;

    public LatestNprRenderScheduler(
        string schedulerName,
        Func<NprRenderRequest, CancellationToken, ValueTask<NprRenderResult>> renderAsync,
        Action<NprRenderSchedulerNotification>? notify = null)
    {
        if (string.IsNullOrWhiteSpace(schedulerName))
        {
            throw new ArgumentException("Scheduler name must not be empty.", nameof(schedulerName));
        }

        Name = schedulerName;
        _renderAsync = renderAsync ?? throw new ArgumentNullException(nameof(renderAsync));
        _notify = notify;
        _thread = new Thread(WorkerLoop)
        {
            IsBackground = true,
            Name = schedulerName
        };
        _thread.Start();
    }

    public string Name { get; }

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
                Notify(
                    NprRenderSchedulerNotificationKind.RequestDroppedBeforeEnqueue,
                    request,
                    LatestRequestedRevision);
                return ValueTask.CompletedTask;
            }

            LatestRequestedRevision = request.Revision;
            _latestRequest = request;
        }

        _signal.Set();
        return ValueTask.CompletedTask;
    }

    private void WorkerLoop()
    {
        while (!_disposeCts.IsCancellationRequested)
        {
            _signal.WaitOne();
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
                var result = _renderAsync(request, cts.Token).AsTask().GetAwaiter().GetResult();

                if (request.Budget.AllowDroppingOldFrames && result.Revision < LatestRequestedRevision)
                {
                    result.Dispose();
                    Notify(
                        NprRenderSchedulerNotificationKind.ResultDroppedAfterRender,
                        request,
                        LatestRequestedRevision,
                        NprRenderStatus.Dropped);
                    PublishDropped(request, NprRenderStatus.Dropped, null);
                    continue;
                }

                LatestCompletedRevision = result.Revision;
                RenderCompleted?.Invoke(result);
            }
            catch (OperationCanceledException)
            {
                Notify(
                    NprRenderSchedulerNotificationKind.RenderCancelled,
                    request,
                    LatestRequestedRevision,
                    NprRenderStatus.Cancelled);
                PublishDropped(request, NprRenderStatus.Cancelled, null);
            }
            catch (Exception exception)
            {
                Notify(
                    NprRenderSchedulerNotificationKind.RenderFailed,
                    request,
                    LatestRequestedRevision,
                    NprRenderStatus.Failed,
                    exception);
                PublishDropped(request, NprRenderStatus.Failed, exception);
            }
        }
    }

    private void PublishDropped(NprRenderRequest request, NprRenderStatus status, Exception? exception)
    {
        var diagnostics = new NprRenderDiagnostics
        {
            Width = request.Width,
            Height = request.Height,
            WorkerCount = request.Budget.ResolveWorkerCount(),
            WorkerBudgetMode = request.Budget.WorkerBudgetMode,
            ProcessorCount = NumericMath.AtLeast(Environment.ProcessorCount, 1),
            Notes = exception?.Message
        };

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

    private void Notify(
        NprRenderSchedulerNotificationKind kind,
        NprRenderRequest request,
        long latestRequestedRevision,
        NprRenderStatus? status = null,
        Exception? exception = null)
    {
        _notify?.Invoke(new NprRenderSchedulerNotification(
            kind,
            Name,
            request.Revision,
            latestRequestedRevision,
            request.ExecutionProfile,
            request.Width,
            request.Height,
            status,
            exception));
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
            Debug.WriteLine($"{Name} worker did not stop within timeout.");
            _notify?.Invoke(new NprRenderSchedulerNotification(
                NprRenderSchedulerNotificationKind.StopTimeout,
                Name,
                LatestRequestedRevision,
                LatestRequestedRevision,
                NprExecutionProfile.FullCpuReference,
                0,
                0));
        }

        _currentRenderCts?.Dispose();
        _disposeCts.Dispose();
        _signal.Dispose();
    }
}
