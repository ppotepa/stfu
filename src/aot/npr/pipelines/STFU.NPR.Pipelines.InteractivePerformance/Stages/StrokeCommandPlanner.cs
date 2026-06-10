using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;

namespace STFU.NPR.Pipeline.InteractivePerformance.Stages;

public static class StrokeCommandPlanner
{
    public static InteractiveStrokeCommand[] BuildCommands(IReadOnlyList<InteractiveCandidateEdge> candidates)
    {
        if (candidates.Count == 0)
        {
            return [];
        }

        var commands = new List<InteractiveStrokeCommand>(candidates.Count);
        foreach (var candidate in candidates)
        {
            if (candidate.ProjectedLength <= 0.25f || candidate.Importance <= 0f)
            {
                continue;
            }

            var width = ComputeWidth(candidate.Role, candidate.ProjectedLength, candidate.Depth, candidate.Importance);
            var opacity = ComputeOpacity(candidate.Importance);
            commands.Add(new InteractiveStrokeCommand(
                SourceEdgeId: candidate.SourceEdgeId,
                Role: candidate.Role,
                X0: candidate.X0,
                Y0: candidate.Y0,
                X1: candidate.X1,
                Y1: candidate.Y1,
                Width: width,
                Opacity: opacity,
                Importance: candidate.Importance,
                StyleKey: candidate.Role));
        }

        return commands.ToArray();
    }

    private static float ComputeWidth(int role, float projectedLength, float depth, float importance)
    {
        var roleWidth = role switch
        {
            0 => 2.4f,
            1 => 1.8f,
            2 => 1.25f,
            _ => 1.1f
        };
        var lengthScale = Math.Clamp(0.75f + projectedLength / 220f, 0.75f, 1.35f);
        var depthScale = Math.Clamp(1.1f - Math.Abs(depth) * 0.08f, 0.65f, 1.15f);
        var importanceScale = Math.Clamp(importance, 0.35f, 2.0f);

        return Math.Max(0.5f, roleWidth * lengthScale * depthScale * importanceScale);
    }

    private static float ComputeOpacity(float importance)
    {
        return Math.Clamp(0.35f + importance * 0.45f, 0.25f, 1.0f);
    }
}
