using STFU.Common.Collections;
using STFU.Common.Math;
using STFU.NPR.Composition;
using STFU.NPR.Graph;
using STFU.NPR.Rendering;
using STFU.NPR.Settings;
using STFU.Parallelism;
using STFU.Strokes;

namespace STFU.NPR.Pipeline.Default.Steps;

public sealed class DefaultBuildInkFrameStep : STFU.NPR.Pipeline.INprStep
{
    [ThreadStatic]
    private static InkPathScratch? s_threadScratch;

    private readonly InkPathScratch _sequentialScratch = new();
    private StyledPathInfo[] _styledPaths = [];
    private int[] _pathSegmentCounts = [];
    private int[] _pathSegmentOffsets = [];
    private StrokeSegment2D[] _segmentScratch = [];
    private int[] _silhouetteSegmentIndices = [];
    private int[] _featureSegmentIndices = [];
    private int[] _boundarySegmentIndices = [];
    private int _previousSilhouetteIndexCount;
    private int _previousFeatureIndexCount;
    private int _previousBoundaryIndexCount;

    public void Execute(STFU.NPR.Pipeline.NprContext context)
    {
        var pathCount = context.Graph.DefaultDrawablePaths.Count;
        if (pathCount == 0)
        {
            context.Frame = StrokeFrame.Empty;
            context.NprFrame = new NprFrame(
                context.Width,
                context.Height,
                new NprPaper(context.Settings.DefaultDrawing.PaperColor, 1f),
                [],
                context.Frame);
            context.Counters.Set("DefaultBuildInkFrameStep.drawablePathCount", 0);
            context.Counters.Set("DefaultBuildInkFrameStep.totalSegments", 0);
            context.Counters.Set("DefaultBuildInkFrameStep.pathsOutput", 0);
            context.Counters.Set("DefaultBuildInkFrameStep.segmentsOutput", 0);
            context.Counters.Set("DefaultBuildInkFrameStep.segmentScratchCapacity", _segmentScratch.Length);
            context.Counters.Set("DefaultBuildInkFrameStep.frameSegmentCount", 0);
            context.Counters.Set("DefaultBuildInkFrameStep.layerIndexClearSilhouette", 0);
            context.Counters.Set("DefaultBuildInkFrameStep.layerIndexClearFeature", 0);
            context.Counters.Set("DefaultBuildInkFrameStep.layerIndexClearBoundary", 0);
            return;
        }

        EnsureCapacity(pathCount);
        var drawing = context.Settings.DefaultDrawing;
        var silhouetteStyle = CreateLineStyle(context, DefaultLineKind.Silhouette);
        var featureStyle = CreateLineStyle(context, DefaultLineKind.Feature);
        var boundaryStyle = CreateLineStyle(context, DefaultLineKind.Boundary);

        var silhouettePathCount = 0;
        var featurePathCount = 0;
        var boundaryPathCount = 0;
        var silhouetteSegmentCount = 0;
        var featureSegmentCount = 0;
        var boundarySegmentCount = 0;

        for (var pathIndex = 0; pathIndex < pathCount; pathIndex++)
        {
            var path = context.Graph.DefaultDrawablePaths[pathIndex];
            var layerIndex = path.Type switch
            {
                DefaultLineKind.Silhouette => 0,
                DefaultLineKind.Feature => 1,
                _ => 2
            };

            var lineStyle = layerIndex switch
            {
                0 => silhouetteStyle,
                1 => featureStyle,
                _ => boundaryStyle
            };

            var seed = context.Settings.Seed + path.StableId * 17;
            var passes = StrokeHumanizationMath.PassCount(drawing.StrokeStyle.ToString());
            var count = CountStyledPathSegments(path, drawing, seed, drawing.StrokeStyle);
            _styledPaths[pathIndex] = new StyledPathInfo(path, lineStyle, layerIndex, passes, drawing, seed, drawing.StrokeStyle);
            _pathSegmentCounts[pathIndex] = count;

            switch (layerIndex)
            {
                case 0:
                    silhouettePathCount++;
                    silhouetteSegmentCount += count;
                    break;
                case 1:
                    featurePathCount++;
                    featureSegmentCount += count;
                    break;
                default:
                    boundaryPathCount++;
                    boundarySegmentCount += count;
                    break;
            }
        }

        var totalSegments = PrefixSums.ExclusiveFromCounts(_pathSegmentCounts.AsSpan(0, pathCount), _pathSegmentOffsets.AsSpan(0, pathCount));
        EnsureSegmentCapacity(totalSegments);

        var parallel = context.WorkerCount > 1 && pathCount >= 256;
        if (parallel)
        {
            DeterministicParallel.ForRanges(
                0,
                pathCount,
                context.WorkerCount,
                context.CancellationToken,
                (startInclusive, endExclusive, _, cancellationToken) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    for (var pathIndex = startInclusive; pathIndex < endExclusive; pathIndex++)
                    {
                        if ((pathIndex & 0x3FF) == 0)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                        }

                        var info = _styledPaths[pathIndex];
                        var offset = _pathSegmentOffsets[pathIndex];
                        var count = _pathSegmentCounts[pathIndex];
                        if (count == 0)
                        {
                            continue;
                        }

                        var scratch = s_threadScratch ??= new InkPathScratch();
                        var written = WriteStyledPathSegments(info, _segmentScratch.AsSpan(offset, count), scratch);
                        if (written != count)
                        {
                            throw new InvalidOperationException($"Styled path write count mismatch for path {pathIndex}: expected {count}, actual {written}.");
                        }
                    }
                },
                minItemsPerRange: 64);
        }
        else
        {
            for (var pathIndex = 0; pathIndex < pathCount; pathIndex++)
            {
                var info = _styledPaths[pathIndex];
                var offset = _pathSegmentOffsets[pathIndex];
                var count = _pathSegmentCounts[pathIndex];
                if (count == 0)
                {
                    continue;
                }

                var written = WriteStyledPathSegments(info, _segmentScratch.AsSpan(offset, count), _sequentialScratch);
                if (written != count)
                {
                    throw new InvalidOperationException($"Styled path write count mismatch for path {pathIndex}: expected {count}, actual {written}.");
                }
            }
        }

        BuildLayerIndices(pathCount);

        var segments = new ArraySliceReadOnlyList<StrokeSegment2D>(_segmentScratch, totalSegments);
        var framePaths = new StrokeSegmentPathList(segments);
        context.Frame = new StrokeFrame(context.Width, context.Height, framePaths, segments);
        context.NprFrame = new NprFrame(
            context.Width,
            context.Height,
            new NprPaper(context.Settings.DefaultDrawing.PaperColor, 1f),
            BuildLayers(
                silhouetteStyle,
                featureStyle,
                boundaryStyle,
                silhouetteSegmentCount,
                featureSegmentCount,
                boundarySegmentCount,
                _segmentScratch,
                _silhouetteSegmentIndices,
                _featureSegmentIndices,
                _boundarySegmentIndices),
            context.Frame);
        context.Counters.Set("DefaultBuildInkFrameStep.drawablePathCount", pathCount);
        context.Counters.Set("DefaultBuildInkFrameStep.totalSegments", totalSegments);
        context.Counters.Set("DefaultBuildInkFrameStep.pathsOutput", pathCount);
        context.Counters.Set("DefaultBuildInkFrameStep.segmentsOutput", totalSegments);
        context.Counters.Set("DefaultBuildInkFrameStep.segmentScratchCapacity", _segmentScratch.Length);
        context.Counters.Set("DefaultBuildInkFrameStep.frameSegmentCount", totalSegments);
        context.Counters.Set("DefaultBuildInkFrameStep.layerIndexClearSilhouette", _previousSilhouetteIndexCount);
        context.Counters.Set("DefaultBuildInkFrameStep.layerIndexClearFeature", _previousFeatureIndexCount);
        context.Counters.Set("DefaultBuildInkFrameStep.layerIndexClearBoundary", _previousBoundaryIndexCount);
    }

    private void EnsureCapacity(int pathCount)
    {
        if (_styledPaths.Length < pathCount)
        {
            _styledPaths = new StyledPathInfo[pathCount];
        }

        if (_pathSegmentCounts.Length < pathCount)
        {
            _pathSegmentCounts = new int[pathCount];
            _pathSegmentOffsets = new int[pathCount];
        }

    }

    private void EnsureSegmentCapacity(int totalSegments)
    {
        if (_segmentScratch.Length < totalSegments)
        {
            _segmentScratch = new StrokeSegment2D[totalSegments];
        }

        if (_silhouetteSegmentIndices.Length < totalSegments)
        {
            _silhouetteSegmentIndices = new int[totalSegments];
            _featureSegmentIndices = new int[totalSegments];
            _boundarySegmentIndices = new int[totalSegments];
        }
    }

    private void BuildLayerIndices(int pathCount)
    {
        var silhouetteCursor = 0;
        var featureCursor = 0;
        var boundaryCursor = 0;

        for (var pathIndex = 0; pathIndex < pathCount; pathIndex++)
        {
            var info = _styledPaths[pathIndex];
            var count = _pathSegmentCounts[pathIndex];
            if (count == 0)
            {
                continue;
            }

            var sourceOffset = _pathSegmentOffsets[pathIndex];
            switch (info.LayerIndex)
            {
                case 0:
                    for (var segmentIndex = 0; segmentIndex < count; segmentIndex++)
                    {
                        _silhouetteSegmentIndices[silhouetteCursor + segmentIndex] = sourceOffset + segmentIndex;
                    }

                    silhouetteCursor += count;
                    break;
                case 1:
                    for (var segmentIndex = 0; segmentIndex < count; segmentIndex++)
                    {
                        _featureSegmentIndices[featureCursor + segmentIndex] = sourceOffset + segmentIndex;
                    }

                    featureCursor += count;
                    break;
                default:
                    for (var segmentIndex = 0; segmentIndex < count; segmentIndex++)
                    {
                        _boundarySegmentIndices[boundaryCursor + segmentIndex] = sourceOffset + segmentIndex;
                    }

                    boundaryCursor += count;
                    break;
            }
        }

        _previousSilhouetteIndexCount = silhouetteCursor;
        _previousFeatureIndexCount = featureCursor;
        _previousBoundaryIndexCount = boundaryCursor;
    }

    private static int CountStyledPathSegments(
        in DefaultProjectedPath path,
        DefaultDrawingSettings drawing,
        int seed,
        DefaultStrokeStyle style)
    {
        if (path.Points.Count < 2)
        {
            return 0;
        }

        var styleName = style.ToString();
        var passes = StrokeHumanizationMath.PassCount(styleName);
        var count = 0;
        for (var pass = 0; pass < passes; pass++)
        {
            for (var i = 1; i < path.Points.Count; i++)
            {
                if (StrokeHumanizationMath.ShouldSkipSegment(styleName, i, seed, pass, drawing.EnableFastNoise))
                {
                    continue;
                }

                count++;
            }
        }

        return count;
    }

    private static int WriteStyledPathSegments(
        in StyledPathInfo info,
        Span<StrokeSegment2D> destination,
        InkPathScratch scratch)
    {
        var path = info.Path;
        if (path.Points.Count < 2)
        {
            return 0;
        }

        var drawing = info.Drawing;
        var style = drawing.StrokeStyle;
        var styleName = style.ToString();
        var comic = style == DefaultStrokeStyle.ComicInk;
        var baseJitter = drawing.Jitter * (path.Type == DefaultLineKind.Feature ? 0.8f : 1f);
        var pressure = comic ? NumericMath.AtLeast((double)drawing.Pressure, 0.54d) : drawing.Pressure;
        var written = 0;
        var pointCount = path.Points.Count;
        scratch.EnsureCapacity(pointCount);
        var startPoints = scratch.StartPoints.AsSpan(0, pointCount);
        var endPoints = scratch.EndPoints.AsSpan(0, pointCount);

        for (var pass = 0; pass < info.Passes; pass++)
        {
            var passStyle = StrokeHumanizationMath.Pass(styleName, baseJitter, pass);
            var alpha = passStyle.Alpha;
            var passJitter = passStyle.Jitter;
            var widthMultiplier = passStyle.WidthMultiplier;

            BuildJitteredPoints(path.Points, startPoints, passJitter, info.Seed + pass * 11, drawing.EnableFastNoise);
            BuildJitteredPoints(path.Points, endPoints, passJitter, info.Seed + pass * 11 + 3, drawing.EnableFastNoise);

            for (var i = 1; i < pointCount; i++)
            {
                if (StrokeHumanizationMath.ShouldSkipSegment(styleName, i, info.Seed, pass, drawing.EnableFastNoise))
                {
                    continue;
                }

                var start = startPoints[i - 1];
                var end = endPoints[i];
                var t = i / (double)NumericMath.AtLeast(pointCount - 1, 1);
                var pressureNoise = StrokeHumanizationMath.PressureNoise(pressure, t, info.Seed, pass, comic, drawing.EnableFastNoise);
                var taper = StrokeHumanizationMath.Taper(t, comic);
                var lineWidth = StrokeHumanizationMath.LineWidth(info.LineStyle.BaseWidth, widthMultiplier, pressureNoise, taper);

                var segmentStyle = new StrokeStyle2D((float)lineWidth, NumericMath.Clamp01(alpha * info.LineStyle.BaseOpacity), info.LineStyle.StrokeColor);
                destination[written++] = new StrokeSegment2D(
                    start,
                    end,
                    segmentStyle,
                    new StrokeMetadata(
                        HashMath.StableSequence31(path.StableId, pass, i),
                        info.LineStyle.LayerName,
                        "DefaultInkSegment",
                        info.LineStyle.IntentText,
                        path.StableId,
                        i,
                        "Visible",
                        info.LineStyle.StyleId,
                        null,
                        info.LineStyle.LayerOrder));
            }
        }

        return written;
    }

    private static void BuildJitteredPoints(
        IReadOnlyList<Point2D> points,
        Span<Point2D> destination,
        float amount,
        int seed,
        bool fastNoise)
    {
        for (var i = 0; i < points.Count; i++)
        {
            destination[i] = JitterPoint(points, i, amount, seed, fastNoise);
        }
    }

    private static StyledLineInfo CreateLineStyle(STFU.NPR.Pipeline.NprContext context, DefaultLineKind lineKind)
    {
        var drawing = context.Settings.DefaultDrawing;
        var intent = ToIntent(lineKind);
        var curveKind = ToCurveKind(lineKind);
        var layerName = context.Style.ResolveOutputLayer(curveKind, intent, VisibilityState.Visible);
        var layerOrder = context.Style.GetLayerOrder(curveKind, intent, VisibilityState.Visible);
        var profile = context.Style.Stroke.FindProfile(curveKind, intent, layerName) ?? context.Style.Stroke.FindProfile(intent);
        var strokeColor = profile?.Color ?? drawing.StrokeColor;
        var baseWidth = NumericMath.AtLeast((profile?.BaseThickness ?? drawing.LineWidth) * context.Style.Stroke.ThicknessScale, 0.35f);
        var baseOpacity = NumericMath.Clamp01((profile?.BaseOpacity ?? 1f) * context.Style.Stroke.OpacityScale);

        return new StyledLineInfo(
            intent.ToString(),
            layerName,
            layerOrder,
            strokeColor,
            baseWidth,
            baseOpacity);
    }

    private static IReadOnlyList<NprLayerFrame> BuildLayers(
        StyledLineInfo silhouetteStyle,
        StyledLineInfo featureStyle,
        StyledLineInfo boundaryStyle,
        int silhouetteSegmentCount,
        int featureSegmentCount,
        int boundarySegmentCount,
        StrokeSegment2D[] segments,
        int[] silhouetteSegmentIndices,
        int[] featureSegmentIndices,
        int[] boundarySegmentIndices)
    {
        var bucketCount = 0;
        if (silhouetteSegmentCount > 0) bucketCount++;
        if (featureSegmentCount > 0) bucketCount++;
        if (boundarySegmentCount > 0) bucketCount++;

        if (bucketCount == 0)
        {
            return [];
        }

        var layers = new NprLayerFrame[bucketCount];
        var written = 0;
        if (silhouetteSegmentCount > 0)
        {
            layers[written++] = BuildLayer(silhouetteStyle, segments, silhouetteSegmentIndices, silhouetteSegmentCount);
        }

        if (featureSegmentCount > 0)
        {
            layers[written++] = BuildLayer(featureStyle, segments, featureSegmentIndices, featureSegmentCount);
        }

        if (boundarySegmentCount > 0)
        {
            layers[written++] = BuildLayer(boundaryStyle, segments, boundarySegmentIndices, boundarySegmentCount);
        }

        return layers;
    }

    private static NprLayerFrame BuildLayer(
        StyledLineInfo style,
        StrokeSegment2D[] segments,
        int[] segmentIndices,
        int segmentCount)
    {
        var layerSegments = new IndexedArrayReadOnlyList<StrokeSegment2D>(segments, segmentIndices, segmentCount);
        var layerPaths = new StrokeSegmentPathList(layerSegments);
        return new NprLayerFrame(
            style.LayerName,
            LayerTitle(style.LayerName),
            NprSceneRole.Foreground,
            style.LayerOrder,
            true,
            1f,
            NprLayerBlendMode.Normal,
            [],
            [],
            layerPaths,
            null,
            layerSegments);
    }

    private static string LayerTitle(string layer)
    {
        return layer switch
        {
            "silhouette" => "Silhouette",
            "feature" => "Feature",
            "boundary" => "Boundary",
            _ => layer
        };
    }

    private static Point2D JitterPoint(IReadOnlyList<Point2D> points, int index, float amount, int seed, bool fastNoise)
    {
        var point = points[index];
        var previous = points[NumericMath.AtLeast(index - 1, 0)];
        var next = points[NumericMath.AtMost(points.Count - 1, index + 1)];
        var jittered = StrokeHumanizationMath.JitterPoint(
            point.X,
            point.Y,
            previous.X,
            previous.Y,
            next.X,
            next.Y,
            index,
            amount,
            seed,
            fastNoise);
        return new Point2D(jittered.X, jittered.Y);
    }

    private static NprStrokeIntent ToIntent(DefaultLineKind lineKind)
    {
        return lineKind switch
        {
            DefaultLineKind.Silhouette => NprStrokeIntent.Silhouette,
            DefaultLineKind.Feature => NprStrokeIntent.Crease,
            _ => NprStrokeIntent.Boundary
        };
    }

    private static FeatureCurveKind ToCurveKind(DefaultLineKind lineKind)
    {
        return lineKind switch
        {
            DefaultLineKind.Silhouette => FeatureCurveKind.Silhouette,
            DefaultLineKind.Feature => FeatureCurveKind.Crease,
            _ => FeatureCurveKind.Boundary
        };
    }

    private readonly record struct StyledPathInfo(
        DefaultProjectedPath Path,
        StyledLineInfo LineStyle,
        int LayerIndex,
        int Passes,
        DefaultDrawingSettings Drawing,
        int Seed,
        DefaultStrokeStyle StrokeStyle)
    {
    }

    private readonly record struct StyledLineInfo(
        string IntentText,
        string LayerName,
        int LayerOrder,
        StrokeColor StrokeColor,
        float BaseWidth,
        float BaseOpacity,
        string StyleId = "DefaultInkSegment");

    private sealed class InkPathScratch
    {
        public Point2D[] StartPoints = [];
        public Point2D[] EndPoints = [];

        public void EnsureCapacity(int pointCount)
        {
            if (StartPoints.Length >= pointCount)
            {
                return;
            }

            var capacity = 4;
            while (capacity < pointCount)
            {
                capacity <<= 1;
            }

            StartPoints = new Point2D[capacity];
            EndPoints = new Point2D[capacity];
        }
    }
}
