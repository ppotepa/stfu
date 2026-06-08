using STFU.Common.Math;
using STFU.NPR.Graph;
using STFU.Strokes;

namespace STFU.NPR.Pipeline.ReferenceQuality.Steps;

public sealed class DefaultApplyDrawProgressStep : STFU.NPR.Pipeline.INprStep
{
    private static readonly Func<Point2D, float> GetX = static point => point.X;
    private static readonly Func<Point2D, float> GetY = static point => point.Y;
    private static readonly Func<float, float, Point2D> CreatePoint = static (x, y) => new Point2D(x, y);

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

        var progress = NumericMath.Clamp01(context.Settings.DefaultDrawing.DrawProgress);
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

            var partial = PathMath.PartialPath(path.Points, remaining, GetX, GetY, CreatePoint);
            if (partial.Count > 1)
            {
                context.Graph.DefaultDrawablePaths.Add(path with
                {
                    Points = partial,
                    Length = PathMath.PathLength(partial, GetX, GetY)
                });
            }

            break;
        }
    }
}