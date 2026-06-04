using STFU.NPR.Graph;

namespace STFU.NPR.Pipeline.Default.Steps;

public sealed class DefaultApplyDrawProgressStep : STFU.NPR.Pipeline.INprStep
{
    public void Execute(STFU.NPR.Pipeline.NprContext context)
    {
        context.Graph.DefaultDrawablePaths.Clear();

        if (context.Graph.DefaultPaths.Count == 0)
        {
            return;
        }

        context.Graph.DefaultDrawablePaths.EnsureCapacity(context.Graph.DefaultPaths.Count);

        var totalLength = 0f;
        foreach (var path in context.Graph.DefaultPaths)
        {
            totalLength += path.Length;
        }
        if (totalLength <= 0f)
        {
            totalLength = 1f;
        }

        var progress = Math.Clamp(context.Settings.DefaultDrawing.DrawProgress, 0f, 1f);
        var remaining = totalLength * progress;

        foreach (var path in context.Graph.DefaultPaths)
        {
            if (remaining <= 0f)
            {
                break;
            }

            if (remaining >= path.Length)
            {
                context.Graph.DefaultDrawablePaths.Add(path);
                remaining -= path.Length;
                continue;
            }

            var partial = DefaultPathMath.PartialPath(path.Points, remaining);
            if (partial.Count > 1)
            {
                context.Graph.DefaultDrawablePaths.Add(path with
                {
                    Points = partial,
                    Length = DefaultPathMath.PathLength(partial)
                });
            }

            break;
        }
    }
}
