using System.Diagnostics;

namespace STFU.Rendering.DirectX.Diagnostics;

public sealed class DirectXGpuTimer
{
    public TimingScope Measure(Action<double> publish)
    {
        return new TimingScope(publish);
    }

    public readonly struct TimingScope : IDisposable
    {
        private readonly Stopwatch _stopwatch;
        private readonly Action<double> _publish;

        public TimingScope(Action<double> publish)
        {
            _publish = publish;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _publish(_stopwatch.Elapsed.TotalMilliseconds);
        }
    }
}
