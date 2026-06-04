using STFU.NPR.Graph;
using STFU.NPR.Pipeline;

namespace STFU.NPR.Visibility;

public sealed class BvhVisibilityResolver : IVisibilityResolver
{
    private readonly SampleVisibilityResolver _inner = new();

    public IReadOnlyList<VisibilitySegment> Resolve(NprContext context, IReadOnlyList<FeatureCurve> curves)
    {
        return _inner.Resolve(context, curves);
    }
}
