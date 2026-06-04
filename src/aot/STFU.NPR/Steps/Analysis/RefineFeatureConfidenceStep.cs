using STFU.NPR.Graph;
using STFU.NPR.Pipeline;

namespace STFU.NPR.Steps.Analysis;

public sealed class RefineFeatureConfidenceStep : INprStep
{
    public void Execute(NprContext context)
    {
        if (context.Graph.Curves.Count == 0)
        {
            return;
        }

        var edgeDerived = context.Graph.Curves
            .Where(IsTargetKind)
            .ToArray();
        if (edgeDerived.Length == 0)
        {
            return;
        }

        var adjacency = BuildAdjacency(edgeDerived);
        var refined = new List<FeatureCurve>(context.Graph.Curves.Count);

        foreach (var curve in context.Graph.Curves)
        {
            if (!IsTargetKind(curve))
            {
                refined.Add(curve);
                continue;
            }

            var compatibleNeighbors = EnumerateNeighbors(adjacency, curve)
                .Where(candidate => candidate.StableId != curve.StableId && IsCompatible(curve.Kind, candidate.Kind))
                .DistinctBy(candidate => candidate.StableId)
                .ToArray();

            if (compatibleNeighbors.Length == 0)
            {
                refined.Add(curve);
                continue;
            }

            var neighborAverage = compatibleNeighbors.Average(candidate => candidate.Confidence);
            var support = Math.Clamp(compatibleNeighbors.Length / 3f, 0f, 1f);
            var refinedConfidence = Math.Clamp(
                curve.Confidence * 0.72f +
                (float)neighborAverage * 0.18f +
                support * 0.10f,
                0f,
                1f);

            refined.Add(curve with { Confidence = refinedConfidence });
        }

        context.Graph.ReplaceCurves(refined);
    }

    private static Dictionary<int, List<FeatureCurve>> BuildAdjacency(IEnumerable<FeatureCurve> curves)
    {
        var byVertex = new Dictionary<int, List<FeatureCurve>>();

        foreach (var curve in curves)
        {
            Add(byVertex, curve.Source.StartVertexIndex, curve);
            Add(byVertex, curve.Source.EndVertexIndex, curve);
        }

        return byVertex;
    }

    private static void Add(Dictionary<int, List<FeatureCurve>> adjacency, int vertexIndex, FeatureCurve curve)
    {
        if (vertexIndex < 0)
        {
            return;
        }

        var key = vertexIndex;
        if (!adjacency.TryGetValue(key, out var bucket))
        {
            bucket = [];
            adjacency.Add(key, bucket);
        }

        bucket.Add(curve);
    }

    private static IEnumerable<FeatureCurve> EnumerateNeighbors(Dictionary<int, List<FeatureCurve>> adjacency, FeatureCurve curve)
    {
        if (curve.Source.StartVertexIndex >= 0 &&
            adjacency.TryGetValue(curve.Source.StartVertexIndex, out var startBucket))
        {
            foreach (var item in startBucket)
            {
                yield return item;
            }
        }

        if (curve.Source.EndVertexIndex >= 0 &&
            adjacency.TryGetValue(curve.Source.EndVertexIndex, out var endBucket))
        {
            foreach (var item in endBucket)
            {
                yield return item;
            }
        }
    }

    private static bool IsTargetKind(FeatureCurve curve)
    {
        return IsTargetKind(curve.Kind);
    }

    private static bool IsTargetKind(FeatureCurveKind kind)
    {
        return kind is FeatureCurveKind.Ridge or FeatureCurveKind.Valley or FeatureCurveKind.SuggestiveContour or FeatureCurveKind.ApparentRidge;
    }

    private static bool IsCompatible(FeatureCurveKind source, FeatureCurveKind neighbor)
    {
        if (source == neighbor)
        {
            return true;
        }

        return source is FeatureCurveKind.Ridge or FeatureCurveKind.Valley &&
            neighbor is FeatureCurveKind.Ridge or FeatureCurveKind.Valley;
    }
}
