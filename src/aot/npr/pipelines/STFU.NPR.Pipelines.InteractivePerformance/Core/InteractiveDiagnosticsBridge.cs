using STFU.NPR.Pipeline;
using STFU.NPR.Graph;

namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public static class InteractiveDiagnosticsBridge
{
    public static void WriteToContext(NprContext context, InteractiveFrameDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(diagnostics);

        HarvestReferenceGraph(context, diagnostics);

        context.Counters.Set("InteractivePerformance.totalFaces", diagnostics.TotalFaces);
        context.Counters.Set("InteractivePerformance.visibleFaces", diagnostics.VisibleFaces);
        context.Counters.Set("InteractivePerformance.visibleFaceRatioPercent", (long)Math.Round(diagnostics.VisibleFaceRatioPercent));
        context.Counters.Set("InteractivePerformance.totalEdges", diagnostics.TotalEdges);
        context.Counters.Set("InteractivePerformance.candidateEdges", diagnostics.CandidateEdges);
        context.Counters.Set("InteractivePerformance.candidateReductionPercent", (long)Math.Round(diagnostics.CandidateReductionPercent));
        context.Counters.Set("InteractivePerformance.cacheHits", diagnostics.CacheHits);
        context.Counters.Set("InteractivePerformance.cacheMisses", diagnostics.CacheMisses);
        context.Counters.Set("InteractivePerformance.usedReferenceFallback", diagnostics.UsedReferenceFallback ? 1 : 0);
    }

    private static void HarvestReferenceGraph(NprContext context, InteractiveFrameDiagnostics diagnostics)
    {
        var graph = context.Graph;
        var totalFaces = graph.Triangles.Count;
        var visibleFaces = CountVisibleFaces(graph.DefaultFaceIdVisibility?.FaceVisible, totalFaces);
        var totalEdges = graph.DefaultFragments.Count > 0
            ? graph.DefaultFragments.Count
            : graph.TopologyEdges.Count;
        var candidateEdges = CountCandidateEdgesForVisibleFaces(graph.DefaultFragments, graph.DefaultFaceIdVisibility?.FaceVisible);

        diagnostics.TotalFaces = totalFaces;
        diagnostics.VisibleFaces = visibleFaces;
        diagnostics.VisibleFaceRatioPercent = totalFaces <= 0 ? 0d : visibleFaces * 100d / totalFaces;
        diagnostics.TotalEdges = totalEdges;
        diagnostics.CandidateEdges = candidateEdges;
        diagnostics.CandidateReductionPercent = totalEdges <= 0 ? 0d : (totalEdges - candidateEdges) * 100d / totalEdges;
    }

    private static int CountVisibleFaces(bool[]? faceVisible, int totalFaces)
    {
        if (totalFaces <= 0)
        {
            return 0;
        }

        if (faceVisible is null || faceVisible.Length == 0)
        {
            return totalFaces;
        }

        var count = 0;
        var limit = Math.Min(totalFaces, faceVisible.Length);
        for (var i = 0; i < limit; i++)
        {
            if (faceVisible[i])
            {
                count++;
            }
        }

        return count;
    }

    private static int CountCandidateEdgesForVisibleFaces(
        IReadOnlyList<DefaultLineFragment> fragments,
        bool[]? faceVisible)
    {
        if (fragments.Count == 0)
        {
            return 0;
        }

        if (faceVisible is null || faceVisible.Length == 0)
        {
            return fragments.Count;
        }

        var count = 0;
        foreach (var fragment in fragments)
        {
            if (IsVisibleFace(fragment.FirstTriangleIndex, faceVisible) ||
                IsVisibleFace(fragment.SecondTriangleIndex, faceVisible))
            {
                count++;
            }
        }

        return count;
    }

    private static bool IsVisibleFace(int face, bool[] faceVisible)
    {
        return face >= 0 && face < faceVisible.Length && faceVisible[face];
    }
}
