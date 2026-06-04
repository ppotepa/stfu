using System.Diagnostics;
using STFU.Rendering.Abstractions.Diagnostics;
using STFU.Rendering.Abstractions.Execution;
using STFU.Rendering.Abstractions.Requests;

namespace STFU.Rendering.Cpu.Backend;

public sealed class FullCpuRenderScheduler : INprRenderScheduler
{
    private readonly FullCpuRenderWorker _worker;
    private readonly object _gate = new();
    private readonly AutoResetEvent _signal = new(false);
    private readonly Thread _thread;
    private readonly CancellationTokenSource _disposeCts = new();
    private CancellationTokenSource? _currentRenderCts;
    private NprRenderRequest? _latestRequest;
    private bool _disposed;

    public FullCpuRenderScheduler(FullCpuRenderWorker worker)
    {
        _worker = worker;
        _thread = new Thread(WorkerLoop)
        {
            IsBackground = true,
            Name = "STFU Full CPU NPR Render Worker"
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
            Debug.WriteLine("FullCpuRenderScheduler worker did not stop within timeout.");
        }

        _currentRenderCts?.Dispose();
        _disposeCts.Dispose();
        _signal.Dispose();
    }
}
