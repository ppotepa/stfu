namespace STFU.NPR.Pipeline.ReferenceQuality.Steps;

internal readonly record struct InkSegmentPlan(
    int PathIndex,
    int SourcePointIndex,
    int PassIndex,
    int LayerIndex,
    int Seed,
    bool Emit)
{
    public static InkSegmentPlan Skipped(int pathIndex, int sourcePointIndex, int passIndex, int layerIndex, int seed)
    {
        return new InkSegmentPlan(pathIndex, sourcePointIndex, passIndex, layerIndex, seed, Emit: false);
    }

    public static InkSegmentPlan Emitted(int pathIndex, int sourcePointIndex, int passIndex, int layerIndex, int seed)
    {
        return new InkSegmentPlan(pathIndex, sourcePointIndex, passIndex, layerIndex, seed, Emit: true);
    }
}