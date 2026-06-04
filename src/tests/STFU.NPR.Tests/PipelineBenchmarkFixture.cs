using System.Diagnostics;
using STFU.NPR.Pipeline;
using STFU.Strokes;

internal static class PipelineBenchmarkFixture
{
    public static (StrokeFrame Frame, double Milliseconds) Measure(INprPipeline pipeline, NprContext context, int warmup = 1, int iterations = 3)
    {
        for (var index = 0; index < warmup; index++)
        {
            pipeline.Execute(context);
        }

        var stopwatch = Stopwatch.StartNew();
        StrokeFrame frame = StrokeFrame.Empty;
        for (var index = 0; index < iterations; index++)
        {
            frame = pipeline.Execute(context);
        }

        stopwatch.Stop();
        return (frame, stopwatch.Elapsed.TotalMilliseconds / Math.Max(1, iterations));
    }
}
