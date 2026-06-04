using System.Text.Json;
using STFU.NPR.Pipeline;

namespace STFU.NPR.Debug;

public static class NprDebugSnapshotBuilder
{
    public static NprDebugSnapshot Create(NprContext context)
    {
        var timings = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var trace in context.StepTraces)
        {
            timings[NormalizeStepName(trace.StepName)] = trace.Milliseconds;
        }

        return new NprDebugSnapshot(
            context.FrameId,
            context.Style.StyleId,
            new NprDebugSnapshotCamera(
                context.Camera.FieldOfViewDegrees,
                [context.Camera.Position.X, context.Camera.Position.Y, context.Camera.Position.Z],
                [context.Camera.Target.X, context.Camera.Target.Y, context.Camera.Target.Z]),
            new NprDebugSnapshotCounts(
                context.Graph.Triangles.Count,
                context.Graph.Curves.Count,
                context.Graph.VisibilitySegments.Count(segment => segment.State == Graph.VisibilityState.Visible),
                context.Graph.VisibilitySegments.Count(segment => segment.State == Graph.VisibilityState.Hidden),
                context.Graph.Candidates.Count,
                context.Graph.StyledStrokes.Count),
            timings);
    }

    public static string ToJson(NprContext context, bool indented = true)
    {
        var snapshot = Create(context);
        var options = new JsonSerializerOptions(NprDebugSnapshotJsonContext.Default.Options)
        {
            WriteIndented = indented
        };
        return JsonSerializer.Serialize(snapshot, new NprDebugSnapshotJsonContext(options).NprDebugSnapshot);
    }

    private static string NormalizeStepName(string stepName)
    {
        return stepName
            .Replace("Step", string.Empty, StringComparison.Ordinal)
            .Replace("Build", string.Empty, StringComparison.Ordinal)
            .Replace("Extract", "features", StringComparison.OrdinalIgnoreCase)
            .Replace("ProjectMesh", "projection", StringComparison.OrdinalIgnoreCase)
            .Replace("ProjectedTriangles", "triangles", StringComparison.OrdinalIgnoreCase)
            .Replace("StrokeFrame", "frame", StringComparison.OrdinalIgnoreCase)
            .Replace("DebugFrame", "debug", StringComparison.OrdinalIgnoreCase)
            .ToLowerInvariant();
    }
}
