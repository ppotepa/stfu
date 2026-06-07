using System.Diagnostics;

namespace STFU.Rendering.DirectX.Diagnostics;

public sealed class DxGpuTimer
{
    private readonly Stopwatch _fallbackStopwatch = new();
    private double _lastElapsedMilliseconds;

    public double LastElapsedMilliseconds => _lastElapsedMilliseconds;

    public void Begin()
    {
        _fallbackStopwatch.Restart();
    }

    public double End()
    {
        _fallbackStopwatch.Stop();
        _lastElapsedMilliseconds = _fallbackStopwatch.Elapsed.TotalMilliseconds;
        return _lastElapsedMilliseconds;
    }
}
