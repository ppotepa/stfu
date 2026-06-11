using System.Diagnostics;
using STFU.Rendering.DirectX.Device;
using Vortice.Direct3D11;

namespace STFU.Rendering.DirectX.Diagnostics;

public sealed class DirectXGpuTimer : IDisposable
{
    private readonly DirectXDevice? _device;
    private readonly bool _enableGpuTiming;
    private bool _disposed;

    public bool UsesGpuTimestampQueries => _device is not null && _enableGpuTiming;

    public DirectXGpuTimer()
    {
    }

    public DirectXGpuTimer(DirectXDevice device, bool enableGpuTiming)
    {
        _device = device;
        _enableGpuTiming = enableGpuTiming && device.Support.SupportsTimestampQueries;
    }

    public TimingScope Measure(Action<double> publish)
    {
        return Measure((milliseconds, _) => publish(milliseconds));
    }

    public TimingScope Measure(Action<double, string> publish)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_device is null || !_enableGpuTiming)
        {
            return TimingScope.CpuFallback(publish);
        }

        try
        {
            return TimingScope.GpuTimestamp(_device, publish);
        }
        catch
        {
            return TimingScope.CpuFallback(publish);
        }
    }

    public void Dispose()
    {
        _disposed = true;
    }

    public sealed class TimingScope : IDisposable
    {
        private readonly DirectXDevice? _device;
        private readonly ID3D11Query? _disjointQuery;
        private readonly ID3D11Query? _startQuery;
        private readonly ID3D11Query? _endQuery;
        private readonly Stopwatch _stopwatch;
        private readonly Action<double, string> _publish;
        private bool _disposed;

        private TimingScope(
            DirectXDevice? device,
            ID3D11Query? disjointQuery,
            ID3D11Query? startQuery,
            ID3D11Query? endQuery,
            Action<double, string> publish)
        {
            _device = device;
            _disjointQuery = disjointQuery;
            _startQuery = startQuery;
            _endQuery = endQuery;
            _publish = publish;
            _stopwatch = Stopwatch.StartNew();
        }

        public static TimingScope CpuFallback(Action<double, string> publish)
        {
            return new TimingScope(null, null, null, null, publish);
        }

        public static TimingScope GpuTimestamp(DirectXDevice device, Action<double, string> publish)
        {
            var disjoint = device.Device.CreateQuery(new QueryDescription(QueryType.TimestampDisjoint, QueryFlags.None));
            var start = device.Device.CreateQuery(new QueryDescription(QueryType.Timestamp, QueryFlags.None));
            var end = device.Device.CreateQuery(new QueryDescription(QueryType.Timestamp, QueryFlags.None));

            device.Context.Begin(disjoint);
            device.Context.End(start);
            return new TimingScope(device, disjoint, start, end, publish);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _stopwatch.Stop();

            if (_device is null ||
                _disjointQuery is null ||
                _startQuery is null ||
                _endQuery is null)
            {
                _publish(_stopwatch.Elapsed.TotalMilliseconds, "CpuWallFallback");
                return;
            }

            try
            {
                _device.Context.End(_endQuery);
                _device.Context.End(_disjointQuery);

                if (TryReadGpuMilliseconds(_device.Context, _disjointQuery, _startQuery, _endQuery, out var gpuMilliseconds))
                {
                    _publish(gpuMilliseconds, "GpuTimestamp");
                }
                else
                {
                    _publish(_stopwatch.Elapsed.TotalMilliseconds, "CpuWallFallback");
                }
            }
            finally
            {
                _endQuery.Dispose();
                _startQuery.Dispose();
                _disjointQuery.Dispose();
            }
        }

        private static unsafe bool TryReadGpuMilliseconds(
            ID3D11DeviceContext context,
            ID3D11Query disjointQuery,
            ID3D11Query startQuery,
            ID3D11Query endQuery,
            out double milliseconds)
        {
            milliseconds = 0;

            QueryDataTimestampDisjoint disjoint;
            ulong start;
            ulong end;

            if (!WaitForData(context, disjointQuery, &disjoint, sizeof(QueryDataTimestampDisjoint)) ||
                !WaitForData(context, startQuery, &start, sizeof(ulong)) ||
                !WaitForData(context, endQuery, &end, sizeof(ulong)) ||
                disjoint.Disjoint ||
                disjoint.Frequency == 0 ||
                end < start)
            {
                return false;
            }

            milliseconds = (end - start) * 1000.0 / disjoint.Frequency;
            return true;
        }

        private static unsafe bool WaitForData(
            ID3D11DeviceContext context,
            ID3D11Query query,
            void* data,
            int dataSize)
        {
            const int MaxPolls = 10_000;
            for (var i = 0; i < MaxPolls; i++)
            {
                var result = context.GetData(query, (IntPtr)data, (uint)dataSize, AsyncGetDataFlags.None);
                if (result.Success)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
