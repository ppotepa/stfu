using System.Diagnostics;
using System.Numerics;
using STFU.Common.Math;
using STFU.Mesh;

namespace STFU.Import.Fbx;

public sealed record FbxAnimationPrebakeCacheOptions(
    double SampleRateHz = 30.0,
    long MemoryBudgetBytes = 256L * 1024 * 1024,
    bool Interpolate = true)
{
    public static FbxAnimationPrebakeCacheOptions Default { get; } = new();
}

public readonly record struct FbxAnimationPrebakeCacheStatus(
    bool Hit,
    bool Interpolated,
    int CachedSamples,
    int RequestedSample);

public sealed class FbxAnimationPrebakeCache : IDisposable
{
    private const int ApproximateMeshVertexBytes = 24;

    private readonly object _gate = new();
    private readonly FbxAnimationPrebakeCacheOptions _options;
    private readonly FbxBakedAnimationSampler _sampler;
    private readonly Dictionary<int, CachedSample> _samples = [];
    private readonly Queue<int> _prioritySamples = [];
    private readonly HashSet<int> _queuedSamples = [];
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _worker;
    private long _accessStamp;
    private int _nextSequentialSample;
    private bool _disposed;

    private FbxAnimationPrebakeCache(
        FbxBakedAnimationSampler sampler,
        int animationIndex,
        double durationSeconds,
        int vertexCount,
        FbxAnimationPrebakeCacheOptions options)
    {
        _sampler = sampler;
        _options = options;
        AnimationIndex = animationIndex;
        DurationSeconds = NumericMath.AtLeast(durationSeconds, 0.001);
        VertexCount = NumericMath.AtLeast(vertexCount, 0);
        SampleRateHz = NumericMath.AtLeast(options.SampleRateHz, 1.0);
        SampleCount = AnimationSamplingMath.SampleCount(DurationSeconds, SampleRateHz);
        MaxSamples = AnimationSamplingMath.MaxSampleCount(
            SampleCount,
            options.MemoryBudgetBytes,
            VertexCount,
            ApproximateMeshVertexBytes);

        _worker = Task.Run(BackgroundLoop);
    }

    public int AnimationIndex { get; }

    public double DurationSeconds { get; }

    public double SampleRateHz { get; }

    public int VertexCount { get; }

    public int SampleCount { get; }

    public int MaxSamples { get; }

    public int CachedSampleCount
    {
        get
        {
            lock (_gate)
            {
                return _samples.Count;
            }
        }
    }

    public Exception? LastError { get; private set; }

    public static FbxAnimationPrebakeCache Start(
        string sourcePath,
        int animationIndex,
        double durationSeconds,
        int vertexCount,
        FbxAnimationPrebakeCacheOptions? options = null)
    {
        var sampler = FbxBakedAnimationSampler.Load(sourcePath);
        return new FbxAnimationPrebakeCache(
            sampler,
            animationIndex,
            durationSeconds,
            vertexCount,
            options ?? FbxAnimationPrebakeCacheOptions.Default);
    }

    public bool TryApply(
        double timeSeconds,
        MeshVertex[] target,
        out FbxAnimationPrebakeCacheStatus status)
    {
        status = default;
        if (_disposed || target.Length != VertexCount)
        {
            return false;
        }

        var normalizedTime = AnimationSamplingMath.PositiveModulo(timeSeconds, DurationSeconds);
        var samplePosition = normalizedTime * SampleRateHz;
        var lowerIndex = ClampSampleIndex(AnimationSamplingMath.LowerSampleIndex(samplePosition));
        var upperIndex = ClampSampleIndex(AnimationSamplingMath.UpperSampleIndex(samplePosition));
        var requestedIndex = AnimationSamplingMath.NearestSampleIndex(samplePosition, lowerIndex, upperIndex);

        CachedSample? lower;
        CachedSample? upper;
        lock (_gate)
        {
            lower = GetSampleNoLock(lowerIndex);
            upper = upperIndex == lowerIndex ? lower : GetSampleNoLock(upperIndex);
            if (lower is null)
            {
                EnqueueSampleNoLock(lowerIndex);
            }

            if (upper is null)
            {
                EnqueueSampleNoLock(upperIndex);
            }

            status = new FbxAnimationPrebakeCacheStatus(
                Hit: lower is not null && (upperIndex == lowerIndex || upper is not null || !_options.Interpolate),
                Interpolated: false,
                CachedSamples: _samples.Count,
                RequestedSample: requestedIndex);
        }

        if (lower is null)
        {
            return false;
        }

        if (!_options.Interpolate || upperIndex == lowerIndex)
        {
            lower.Vertices.CopyTo(target, 0);
            status = status with { Hit = true, Interpolated = false };
            return true;
        }

        if (upper is null)
        {
            return false;
        }

        var t = AnimationSamplingMath.SampleInterpolationT(samplePosition, lowerIndex, upperIndex);
        Interpolate(lower.Vertices, upper.Vertices, target, t);
        status = status with { Hit = true, Interpolated = true };
        return true;
    }

    public void Seed(double timeSeconds, IReadOnlyList<MeshVertex> vertices)
    {
        if (_disposed || vertices.Count != VertexCount)
        {
            return;
        }

        var normalizedTime = AnimationSamplingMath.PositiveModulo(timeSeconds, DurationSeconds);
        var samplePosition = normalizedTime * SampleRateHz;
        var sampleIndex = ClampSampleIndex(AnimationSamplingMath.RoundedSampleIndex(samplePosition));
        var sampleTime = GetSampleTime(sampleIndex);
        if (NumericMath.Abs(sampleTime - normalizedTime) > 0.0001)
        {
            return;
        }

        var copy = CopyVertices(vertices);
        lock (_gate)
        {
            AddSampleNoLock(sampleIndex, sampleTime, copy);
        }
    }

    public bool WaitForWarmCache(TimeSpan timeout)
    {
        var targetSamples = NumericMath.AtMost(SampleCount, MaxSamples);
        var watch = Stopwatch.StartNew();
        while (watch.Elapsed < timeout)
        {
            lock (_gate)
            {
                if (_samples.Count >= targetSamples || LastError is not null)
                {
                    return LastError is null;
                }
            }

            Thread.Sleep(10);
        }

        return CachedSampleCount >= targetSamples && LastError is null;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cts.Cancel();
        try
        {
            _worker.Wait(TimeSpan.FromMilliseconds(500));
        }
        catch (AggregateException)
        {
        }
        catch (ObjectDisposedException)
        {
        }

        _cts.Dispose();
    }

    private void BackgroundLoop()
    {
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                var sampleIndex = TakeNextSample();
                if (sampleIndex < 0)
                {
                    return;
                }

                var timeSeconds = GetSampleTime(sampleIndex);
                var mesh = _sampler.BakeCombinedMesh(AnimationIndex, timeSeconds);
                if (mesh.Vertices.Count != VertexCount)
                {
                    throw new InvalidOperationException(
                        $"FBX prebake vertex count changed from {VertexCount} to {mesh.Vertices.Count}.");
                }

                var copy = CopyVertices(mesh.Vertices);
                lock (_gate)
                {
                    AddSampleNoLock(sampleIndex, timeSeconds, copy);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            LastError = exception;
        }
        finally
        {
            _sampler.Dispose();
        }
    }

    private int TakeNextSample()
    {
        lock (_gate)
        {
            while (_prioritySamples.Count > 0)
            {
                var sampleIndex = _prioritySamples.Dequeue();
                _queuedSamples.Remove(sampleIndex);
                if (!_samples.ContainsKey(sampleIndex))
                {
                    return sampleIndex;
                }
            }

            while (_nextSequentialSample < SampleCount)
            {
                var sampleIndex = _nextSequentialSample++;
                if (!_samples.ContainsKey(sampleIndex))
                {
                    return sampleIndex;
                }
            }
        }

        return -1;
    }

    private CachedSample? GetSampleNoLock(int sampleIndex)
    {
        if (!_samples.TryGetValue(sampleIndex, out var sample))
        {
            return null;
        }

        sample.LastAccessStamp = ++_accessStamp;
        return sample;
    }

    private void EnqueueSampleNoLock(int sampleIndex)
    {
        if (_samples.ContainsKey(sampleIndex) || !_queuedSamples.Add(sampleIndex))
        {
            return;
        }

        _prioritySamples.Enqueue(sampleIndex);
    }

    private void AddSampleNoLock(int sampleIndex, double timeSeconds, MeshVertex[] vertices)
    {
        _samples[sampleIndex] = new CachedSample(sampleIndex, timeSeconds, vertices, ++_accessStamp);
        while (_samples.Count > MaxSamples)
        {
            var victim = _samples.Values
                .OrderBy(sample => sample.LastAccessStamp)
                .FirstOrDefault(sample => sample.Index != sampleIndex);
            if (victim is null)
            {
                return;
            }

            _samples.Remove(victim.Index);
        }
    }

    private int ClampSampleIndex(int sampleIndex) => AnimationSamplingMath.ClampSampleIndex(sampleIndex, SampleCount);

    private double GetSampleTime(int sampleIndex) => AnimationSamplingMath.SampleTime(sampleIndex, SampleRateHz, DurationSeconds);

    private static MeshVertex[] CopyVertices(IReadOnlyList<MeshVertex> vertices)
    {
        var copy = new MeshVertex[vertices.Count];
        for (var i = 0; i < vertices.Count; i++)
        {
            copy[i] = vertices[i];
        }

        return copy;
    }

    private static void Interpolate(
        ReadOnlySpan<MeshVertex> lower,
        ReadOnlySpan<MeshVertex> upper,
        Span<MeshVertex> target,
        float t)
    {
        for (var i = 0; i < target.Length; i++)
        {
            var position = Vector3.Lerp(lower[i].Position, upper[i].Position, t);
            var normal = AnimationSamplingMath.InterpolateNormal(lower[i].Normal, upper[i].Normal, t, lower[i].Normal);

            target[i] = new MeshVertex(position, normal);
        }
    }

    private sealed class CachedSample(
        int index,
        double timeSeconds,
        MeshVertex[] vertices,
        long lastAccessStamp)
    {
        public int Index { get; } = index;

        public double TimeSeconds { get; } = timeSeconds;

        public MeshVertex[] Vertices { get; } = vertices;

        public long LastAccessStamp { get; set; } = lastAccessStamp;
    }
}
