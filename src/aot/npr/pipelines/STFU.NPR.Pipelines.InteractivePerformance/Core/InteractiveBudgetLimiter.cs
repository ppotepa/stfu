using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;

namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public static class InteractiveBudgetLimiter
{
    public static InteractiveCandidateEdge[] LimitCandidateEdges(
        IReadOnlyList<InteractiveCandidateEdge> edges,
        int maxEdges)
    {
        if (edges.Count == 0)
        {
            return [];
        }

        if (maxEdges <= 0 || edges.Count <= maxEdges)
        {
            return edges is InteractiveCandidateEdge[] array ? array : edges.ToArray();
        }

        return edges
            .OrderByDescending(edge => edge.Importance)
            .ThenByDescending(edge => edge.ProjectedLength)
            .ThenBy(edge => edge.SourceEdgeId)
            .Take(maxEdges)
            .ToArray();
    }

    public static InteractiveStrokeCommand[] LimitStrokeCommands(
        IReadOnlyList<InteractiveStrokeCommand> commands,
        int maxCommands)
    {
        if (commands.Count == 0)
        {
            return [];
        }

        if (maxCommands <= 0 || commands.Count <= maxCommands)
        {
            return commands is InteractiveStrokeCommand[] array ? array : commands.ToArray();
        }

        return commands
            .OrderByDescending(command => command.Importance)
            .ThenByDescending(command => SegmentLengthSquared(command.X0, command.Y0, command.X1, command.Y1))
            .ThenBy(command => command.SourceEdgeId)
            .Take(maxCommands)
            .ToArray();
    }

    public static InteractiveVisibleStrokeSegment[] LimitVisibleSegments(
        IReadOnlyList<InteractiveVisibleStrokeSegment> segments,
        int maxSegments)
    {
        if (segments.Count == 0)
        {
            return [];
        }

        if (maxSegments <= 0 || segments.Count <= maxSegments)
        {
            return segments is InteractiveVisibleStrokeSegment[] array ? array : segments.ToArray();
        }

        return segments
            .OrderByDescending(segment => segment.Importance)
            .ThenByDescending(segment => segment.Visibility)
            .ThenByDescending(segment => SegmentLengthSquared(segment.X0, segment.Y0, segment.X1, segment.Y1))
            .ThenBy(segment => segment.SourceEdgeId)
            .Take(maxSegments)
            .ToArray();
    }

    private static float SegmentLengthSquared(float x0, float y0, float x1, float y1)
    {
        var dx = x1 - x0;
        var dy = y1 - y0;
        return dx * dx + dy * dy;
    }
}
