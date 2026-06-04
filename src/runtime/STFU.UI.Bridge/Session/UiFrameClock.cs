using System.Diagnostics;

namespace STFU.UI.Bridge.Session;

public sealed class UiFrameClock
{
    private long _sampleStartTicks = Stopwatch.GetTimestamp();
    private int _sampleFrameCount;

    public double LastFps { get; private set; }

    public double RecordFrame()
    {
        _sampleFrameCount++;

        var nowTicks = Stopwatch.GetTimestamp();
        var elapsedSeconds = (nowTicks - _sampleStartTicks) / (double)Stopwatch.Frequency;
        if (elapsedSeconds <= 0)
        {
            return LastFps;
        }

        var sampledFps = _sampleFrameCount / elapsedSeconds;
        if (elapsedSeconds >= 1.0)
        {
            LastFps = sampledFps;
            _sampleFrameCount = 0;
            _sampleStartTicks = nowTicks;
        }

        return LastFps > 0 ? LastFps : sampledFps;
    }
}
