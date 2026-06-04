using STFU.NPR.Graph;
using STFU.NPR.Pipeline;

namespace STFU.NPR.Visibility;

public interface IVisibilityResolver
{
    IReadOnlyList<VisibilitySegment> Resolve(NprContext context, IReadOnlyList<FeatureCurve> curves);
}
