using STFU.NPR.Pipeline;
using STFU.Strokes;

namespace STFU.NPR.Steps.Strokes;

public sealed class BuildStrokeFrameStep : INprStep
{
    public void Execute(NprContext context)
    {
        var paths = new List<StrokePath2D>(context.Graph.Strokes.Count);

        foreach (var stroke in context.Graph.Strokes)
        {
            paths.Add(new StrokePath2D(
                stroke.Points.ToArray(),
                new StrokeStyle2D(stroke.Thickness, stroke.Opacity, stroke.Color)));
        }

        context.Frame = new StrokeFrame(context.Width, context.Height, paths);
    }
}
