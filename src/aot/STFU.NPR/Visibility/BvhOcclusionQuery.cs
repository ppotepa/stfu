using STFU.NPR.Pipeline;
using STFU.Strokes;

namespace STFU.NPR.Visibility;

public sealed class BvhOcclusionQuery : IOcclusionQuery
{
    private int _cachedFrameId = -1;
    private ScreenSpaceBvh? _bvh;

    public bool IsOccluded(NprContext context, Point2D point, float lineDepth)
    {
        EnsureBuilt(context);
        return _bvh is not null &&
            _bvh.IsOccluded(point, lineDepth, context.Settings.HiddenLineDepthBias, context.Graph.Vertices);
    }

    private void EnsureBuilt(NprContext context)
    {
        if (_bvh is not null && _cachedFrameId == context.FrameId)
        {
            return;
        }

        _bvh = ScreenSpaceBvh.Build(context.Graph.Triangles, context.Graph.Vertices);
        _cachedFrameId = context.FrameId;
    }
}
