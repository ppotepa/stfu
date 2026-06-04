using STFU.Strokes;

internal sealed class NprSnapshotTest
{
    public void AssertStable(StrokeFrame baseline, StrokeFrame candidate, float maxMeanEndpointDelta)
    {
        var delta = VisualRegressionMetric.MeanEndpointDelta(baseline, candidate);
        if (float.IsInfinity(delta) || delta > maxMeanEndpointDelta)
        {
            throw new InvalidOperationException(
                $"Snapshot regression exceeded threshold. Mean endpoint delta: {delta:0.###}, limit: {maxMeanEndpointDelta:0.###}.");
        }
    }
}
