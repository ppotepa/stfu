using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;
using STFU.Strokes;

namespace STFU.NPR.Pipeline.InteractivePerformance.Stages;

public sealed record InteractiveStrokeFrameBuildOptions
{
    public bool ClampToViewport { get; init; } = true;

    public int MaxSegments { get; init; }

    public float MinimumSegmentLengthSquared { get; init; } = 0.25f;
}

public static class InteractiveStrokeFrameBuilder
{
    public static StrokeFrame BuildFrame(
        IReadOnlyList<InteractiveVisibleStrokeSegment> visibleSegments,
        int width,
        int height,
        InteractiveQualityMode qualityMode,
        InteractiveStrokeFrameBuildOptions? options = null)
    {
        options ??= new InteractiveStrokeFrameBuildOptions();
        width = Math.Max(1, width);
        height = Math.Max(1, height);

        if (visibleSegments.Count == 0)
        {
            return new StrokeFrame(width, height, [], []);
        }

        var maxSegments = ResolveMaxSegments(visibleSegments.Count, qualityMode, options.MaxSegments);
        if (maxSegments <= 0)
        {
            return new StrokeFrame(width, height, [], []);
        }

        var stride = ResolveStride(visibleSegments.Count, maxSegments);
        var output = new List<StrokeSegment2D>(Math.Min(visibleSegments.Count, maxSegments));

        for (var sourceIndex = 0; sourceIndex < visibleSegments.Count && output.Count < maxSegments; sourceIndex += stride)
        {
            var visible = visibleSegments[sourceIndex];
            if (!TryBuildSegment(visible, width, height, sourceIndex, output.Count, options, out var segment))
            {
                continue;
            }

            output.Add(segment);
        }

        if (output.Count == 0)
        {
            return new StrokeFrame(width, height, [], []);
        }

        var segments = output.ToArray();
        return new StrokeFrame(width, height, new StrokeSegmentPathList(segments), segments);
    }

    public static int ResolveMaxSegments(
        int sourceSegmentCount,
        InteractiveQualityMode qualityMode,
        int explicitMaxSegments = 0)
    {
        if (sourceSegmentCount <= 0)
        {
            return 0;
        }

        var qualityMax = qualityMode switch
        {
            InteractiveQualityMode.FastPreview => 4096,
            InteractiveQualityMode.BalancedViewport or InteractiveQualityMode.Auto => 16384,
            InteractiveQualityMode.QualityViewport => sourceSegmentCount,
            _ => 16384
        };

        var max = explicitMaxSegments > 0
            ? Math.Min(qualityMax, explicitMaxSegments)
            : qualityMax;

        return Math.Min(sourceSegmentCount, Math.Max(1, max));
    }

    public static StrokeStyle2D ResolveStyle(InteractiveVisibleStrokeSegment segment)
    {
        var role = ResolveRole(segment.Role);
        var visibility = Math.Clamp(segment.Visibility, 0f, 1f);
        var importance = Math.Clamp(segment.Importance, 0.15f, 3.0f);
        var opacity = Math.Clamp(role.BaseOpacity * (0.45f + visibility * 0.55f), 0.08f, 1.0f);
        var thickness = Math.Clamp(role.BaseThickness * (0.7f + importance * 0.3f), 0.45f, 12.0f);

        return new StrokeStyle2D(thickness, opacity, role.Color);
    }

    public static StrokeMetadata BuildMetadata(
        InteractiveVisibleStrokeSegment segment,
        int sourceIndex,
        int outputIndex)
    {
        var role = ResolveRole(segment.Role);
        var stableId = BuildStableId(segment, sourceIndex, outputIndex);
        return new StrokeMetadata(
            StableId: stableId,
            Layer: role.LayerName,
            SourceKind: "InteractivePerformance",
            Intent: role.IntentName,
            SourceFeatureId: segment.SourceEdgeId,
            SourceSegmentId: sourceIndex,
            Visibility: "visible",
            StyleId: "interactive-preview",
            Variant: role.Variant,
            LayerOrder: role.LayerOrder);
    }

    public static string ResolveRoleName(int role) => ResolveRole(role).IntentName;

    private static int ResolveStride(int sourceSegmentCount, int maxSegments)
    {
        if (maxSegments <= 0 || sourceSegmentCount <= maxSegments)
        {
            return 1;
        }

        return Math.Max(1, (int)Math.Ceiling(sourceSegmentCount / (double)maxSegments));
    }

    private static bool TryBuildSegment(
        InteractiveVisibleStrokeSegment segment,
        int width,
        int height,
        int sourceIndex,
        int outputIndex,
        InteractiveStrokeFrameBuildOptions options,
        out StrokeSegment2D output)
    {
        output = default;

        if (!IsFinite(segment.X0) || !IsFinite(segment.Y0) ||
            !IsFinite(segment.X1) || !IsFinite(segment.Y1))
        {
            return false;
        }

        var start = new Point2D(segment.X0, segment.Y0);
        var end = new Point2D(segment.X1, segment.Y1);
        if (options.ClampToViewport)
        {
            start = ClampToViewport(start, width, height);
            end = ClampToViewport(end, width, height);
        }

        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        if (dx * dx + dy * dy <= Math.Max(0.0001f, options.MinimumSegmentLengthSquared))
        {
            return false;
        }

        var style = ResolveStyle(segment);
        if (style.Thickness <= 0f || style.Opacity <= 0f)
        {
            return false;
        }

        var metadata = BuildMetadata(segment, sourceIndex, outputIndex);
        var pressure = Math.Clamp(0.35f + segment.Importance * 0.25f, 0.2f, 1.0f);
        var richStart = new StrokePoint2D(start, style.Thickness, style.Opacity, pressure);
        var richEnd = new StrokePoint2D(end, style.Thickness, style.Opacity, pressure);

        output = new StrokeSegment2D(start, end, style, metadata, richStart, richEnd);
        return true;
    }

    private static Point2D ClampToViewport(Point2D point, int width, int height)
    {
        var maxX = Math.Max(0, width - 1);
        var maxY = Math.Max(0, height - 1);
        return new Point2D(
            Math.Clamp(point.X, 0, maxX),
            Math.Clamp(point.Y, 0, maxY));
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static int BuildStableId(
        InteractiveVisibleStrokeSegment segment,
        int sourceIndex,
        int outputIndex)
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + segment.SourceEdgeId;
            hash = hash * 31 + segment.Role;
            hash = hash * 31 + sourceIndex;
            hash = hash * 31 + outputIndex;
            return hash;
        }
    }

    private static InteractiveStrokeRole ResolveRole(int role)
    {
        return role switch
        {
            0 => new InteractiveStrokeRole(
                IntentName: "Silhouette",
                LayerName: "interactive:silhouette",
                Variant: "contour",
                LayerOrder: 0,
                BaseThickness: 2.4f,
                BaseOpacity: 0.95f,
                Color: new StrokeColor(18, 18, 18)),
            1 => new InteractiveStrokeRole(
                IntentName: "Boundary",
                LayerName: "interactive:boundary",
                Variant: "boundary",
                LayerOrder: 1,
                BaseThickness: 1.8f,
                BaseOpacity: 0.9f,
                Color: new StrokeColor(28, 28, 28)),
            2 => new InteractiveStrokeRole(
                IntentName: "Feature",
                LayerName: "interactive:feature",
                Variant: "feature",
                LayerOrder: 2,
                BaseThickness: 1.25f,
                BaseOpacity: 0.78f,
                Color: new StrokeColor(46, 46, 46)),
            3 => new InteractiveStrokeRole(
                IntentName: "Crease",
                LayerName: "interactive:crease",
                Variant: "crease",
                LayerOrder: 3,
                BaseThickness: 1.05f,
                BaseOpacity: 0.72f,
                Color: new StrokeColor(58, 58, 58)),
            _ => new InteractiveStrokeRole(
                IntentName: "Accent",
                LayerName: "interactive:accent",
                Variant: "accent",
                LayerOrder: 4,
                BaseThickness: 0.95f,
                BaseOpacity: 0.65f,
                Color: new StrokeColor(68, 68, 68))
        };
    }

    private readonly record struct InteractiveStrokeRole(
        string IntentName,
        string LayerName,
        string Variant,
        int LayerOrder,
        float BaseThickness,
        float BaseOpacity,
        StrokeColor Color);
}
