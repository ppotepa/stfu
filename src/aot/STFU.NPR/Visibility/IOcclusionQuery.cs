using STFU.NPR.Pipeline;
using STFU.Strokes;

namespace STFU.NPR.Visibility;

public interface IOcclusionQuery
{
    bool IsOccluded(NprContext context, Point2D point, float lineDepth);
}
