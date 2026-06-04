using STFU.NPR.Pipeline;

namespace STFU.NPR.Steps.Analysis;

public sealed class ResolveCurveVisibilityStep : INprStep
{
    private readonly ApplyApproximateOcclusionStep _inner = new();

    public void Execute(NprContext context)
    {
        _inner.Execute(context);
    }
}
