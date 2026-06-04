using STFU.Common.Primitives;
using STFU.Strokes;

namespace STFU.NPR.Graph;

public readonly record struct VisibilitySegment(
    int StableId,
    int FeatureCurveId,
    FeatureCurveKind Kind,
    NprStrokeIntent Intent,
    VisibilityState State,
    float StartT,
    float EndT,
    Point2D Start,
    Point2D End,
    float Depth,
    float Shade,
    float Importance,
    float Confidence,
    HatchLayerKind? HatchLayerKind = null,
    EntityId EntityId = default)
{
    public FeatureLine ToFeatureLine()
    {
        return new FeatureLine(StableId, Intent, Start, End, Depth, Shade, Importance)
        {
            EntityId = EntityId
        };
    }
}
