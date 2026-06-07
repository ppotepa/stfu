using Avalonia.Threading;

namespace STFU.UI;

internal sealed class ViewportFrameLoop : IDisposable
{
    private readonly Action _tick;
    private readonly DispatcherTimer _timer;
    private bool _tickQueued;
    private bool _disposed;

    public ViewportFrameLoop(Action tick)
    {
        _tick = tick;
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _timer.Tick += (_, _) => RunTick();
    }

    public bool IsRunning => _timer.IsEnabled;

    public void Start()
    {
        if (_disposed || _timer.IsEnabled)
        {
            return;
        }

        _timer.Start();
    }

    public void Stop()
    {
        if (!_timer.IsEnabled)
        {
            return;
        }

        _timer.Stop();
    }

    public void RequestImmediateTick()
    {
        if (_disposed || _tickQueued)
        {
            return;
        }

        _tickQueued = true;
        Dispatcher.UIThread.Post(
            () =>
            {
                _tickQueued = false;
                RunTick();
            },
            DispatcherPriority.Render);
    }

    private void RunTick()
    {
        if (_disposed)
        {
            return;
        }

        _tick();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Stop();
    }
}
