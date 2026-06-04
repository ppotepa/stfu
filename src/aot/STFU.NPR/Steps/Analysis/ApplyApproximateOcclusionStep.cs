using STFU.NPR.Graph;
using STFU.NPR.Pipeline;
using STFU.NPR.Visibility;

namespace STFU.NPR.Steps.Analysis;

public sealed class ApplyApproximateOcclusionStep : INprStep
{
    public void Execute(NprContext context)
    {
        if (context.Graph.Curves.Count == 0 || context.Graph.Triangles.Count == 0)
        {
            return;
        }

        context.Graph.ReplaceVisibilitySegments(context.VisibilityResolver.Resolve(context, context.Graph.Curves));
    }
}
