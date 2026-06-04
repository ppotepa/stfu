using STFU.Common.Primitives;
using STFU.Strokes;

namespace STFU.NPR.Graph;

public sealed record FeatureCurve(
    int StableId,
    FeatureCurveKind Kind,
    NprStrokeIntent Intent,
    IReadOnlyList<FeaturePoint> Points,
    FeatureCurveSource Source,
    float Shade,
    float Importance,
    float Confidence,
    FeatureCurveFlags Flags)
{
    public CurveParameterRange ParameterRange { get; init; } = CurveParameterRange.Normalized;

    public HatchLayerKind? HatchLayerKind { get; init; }

    public EntityId EntityId { get; init; } = EntityId.None;

    public float AverageDepth
    {
        get
        {
            if (Points.Count == 0)
            {
                return 0f;
            }

            var total = 0f;
            for (var i = 0; i < Points.Count; i++)
            {
                total += Points[i].Depth;
            }

            return total / Points.Count;
        }
    }

    public FeatureLine ToFeatureLine()
    {
        if (Points.Count < 2)
        {
            throw new InvalidOperationException("FeatureCurve requires at least two points.");
        }

        return new FeatureLine(
            StableId,
            Intent,
            Points[0].ScreenPosition,
            Points[^1].ScreenPosition,
            AverageDepth,
            Shade,
            Importance)
        {
            EntityId = EntityId
        };
    }

    public static FeatureCurve FromLine(
        int stableId,
        FeatureCurveKind kind,
        NprStrokeIntent intent,
        FeaturePoint start,
        FeaturePoint end,
        FeatureCurveSource source,
        float shade,
        float importance,
        float confidence = 1f,
        FeatureCurveFlags flags = FeatureCurveFlags.None,
        HatchLayerKind? hatchLayerKind = null,
        EntityId entityId = default)
    {
        return new FeatureCurve(
            stableId,
            kind,
            intent,
            [start, end],
            source,
            shade,
            importance,
            Math.Clamp(confidence, 0f, 1f),
            flags)
        {
            HatchLayerKind = hatchLayerKind,
            EntityId = entityId
        };
    }
}
