using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;

namespace STFU.NPR.Pipeline.InteractivePerformance.Stages;

public static class VisibleStrokeSegmentPlanner
{
    public static InteractiveVisibleStrokeSegment[] BuildSegments(
        IReadOnlyList<InteractiveStrokeCommand> commands,
        InteractiveQualityMode qualityMode)
    {
        if (commands.Count == 0)
        {
            return [];
        }

        var maxSegments = ResolveMaxSegments(commands.Count, qualityMode);
        var stride = ResolveStride(commands.Count, maxSegments);
        var segments = new List<InteractiveVisibleStrokeSegment>(Math.Min(commands.Count, maxSegments));

        for (var index = 0; index < commands.Count && segments.Count < maxSegments; index += stride)
        {
            var command = commands[index];
            if (!IsDrawable(command))
            {
                continue;
            }

            segments.Add(ToVisibleSegment(command));
        }

        return segments.ToArray();
    }

    public static int ResolveMaxSegments(int commandCount, InteractiveQualityMode qualityMode)
    {
        if (commandCount <= 0)
        {
            return 0;
        }

        return qualityMode switch
        {
            InteractiveQualityMode.FastPreview => Math.Min(commandCount, 4096),
            InteractiveQualityMode.BalancedViewport or InteractiveQualityMode.Auto => Math.Min(commandCount, 16384),
            InteractiveQualityMode.QualityViewport => commandCount,
            _ => Math.Min(commandCount, 16384)
        };
    }

    private static int ResolveStride(int commandCount, int maxSegments)
    {
        if (maxSegments <= 0 || commandCount <= maxSegments)
        {
            return 1;
        }

        return Math.Max(1, (int)Math.Ceiling(commandCount / (double)maxSegments));
    }

    private static bool IsDrawable(InteractiveStrokeCommand command)
    {
        if (command.Opacity <= 0f || command.Width <= 0f)
        {
            return false;
        }

        var dx = command.X1 - command.X0;
        var dy = command.Y1 - command.Y0;
        return dx * dx + dy * dy > 0.25f;
    }

    private static InteractiveVisibleStrokeSegment ToVisibleSegment(InteractiveStrokeCommand command)
    {
        var visibility = Math.Clamp(command.Opacity, 0f, 1f);
        var importance = Math.Clamp(command.Importance, 0f, 4f);

        return new InteractiveVisibleStrokeSegment(
            SourceEdgeId: command.SourceEdgeId,
            Role: command.Role,
            X0: command.X0,
            Y0: command.Y0,
            X1: command.X1,
            Y1: command.Y1,
            Visibility: visibility,
            Importance: importance);
    }
}
