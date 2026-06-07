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
    private readonly InkPathScratch _sequentialScratch = new();
    private StyledPathInfo[] _styledPaths = [];
    private int[] _pathSegmentCounts = [];
    private int[] _pathSegmentOffsets = [];
    private int[] _pathEmitOffsets = [];
    private StrokeSegment2D[] _segmentScratch = [];
    private byte[] _segmentEmitFlags = [];
    private InkSegmentPlan[] _segmentPlans = [];
    private Point2D[] _pathPrecomputedStartPoints = [];
    private Point2D[] _pathPrecomputedEndPoints = [];
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
            context.Counters.Set("DefaultBuildInkFrameStep.emitCandidateCount", 0);
            context.Counters.Set("DefaultBuildInkFrameStep.emitFlagCapacity", _segmentEmitFlags.Length);
            context.Counters.Set("DefaultBuildInkFrameStep.precomputedPointCapacity", _pathPrecomputedStartPoints.Length);
            _previousSilhouetteIndexCount = 0;
            _previousFeatureIndexCount = 0;
            _previousBoundaryIndexCount = 0;
            return;
        }

        EnsureCapacity(pathCount);
        var drawing = context.Settings.DefaultDrawing;
        var silhouetteStyle = CreateLineStyle(context, DefaultLineKind.Silhouette);
        var featureStyle = CreateLineStyle(context, DefaultLineKind.Feature);
        var boundaryStyle = CreateLineStyle(context, DefaultLineKind.Boundary);

        var styleName = drawing.StrokeStyle.ToString();
        var passes = StrokeHumanizationMath.PassCount(styleName);

        var silhouettePathCount = 0;
        var featurePathCount = 0;
        var boundaryPathCount = 0;
        var silhouetteSegmentCount = 0;
        var featureSegmentCount = 0;
        var boundarySegmentCount = 0;
        var emitOffset = 0;

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
            var pathEmitOffset = emitOffset;
            var count = CountStyledPathSegments(
                path,
                drawing,
                seed,
                styleName,
                passes,
                ref emitOffset,
                _sequentialScratch);
            _styledPaths[pathIndex] = new StyledPathInfo(path, lineStyle, layerIndex, passes, drawing, seed, styleName, drawing.StrokeStyle);
            _pathEmitOffsets[pathIndex] = pathEmitOffset;
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

                        var written = WriteStyledPathSegments(
                            info,
                            _segmentScratch.AsSpan(offset, count),
                            _pathEmitOffsets[pathIndex]);
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

                var written = WriteStyledPathSegments(
                    info,
                    _segmentScratch.AsSpan(offset, count),
                    _pathEmitOffsets[pathIndex]);
                if (written != count)
                {
                    throw new InvalidOperationException($"Styled path write count mismatch for path {pathIndex}: expected {count}, actual {written}.");
                }
            }
        }

        if (_previousSilhouetteIndexCount > 0)
        {
            Array.Clear(_silhouetteSegmentIndices, 0, _previousSilhouetteIndexCount);
        }

        if (_previousFeatureIndexCount > 0)
        {
            Array.Clear(_featureSegmentIndices, 0, _previousFeatureIndexCount);
        }

        if (_previousBoundaryIndexCount > 0)
        {
            Array.Clear(_boundarySegmentIndices, 0, _previousBoundaryIndexCount);
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
        context.Counters.Set("DefaultBuildInkFrameStep.emitCandidateCount", emitOffset);
        context.Counters.Set("DefaultBuildInkFrameStep.emitFlagCapacity", _segmentEmitFlags.Length);
        context.Counters.Set("DefaultBuildInkFrameStep.precomputedPointCapacity", _pathPrecomputedStartPoints.Length);
    }

    private void EnsureCapacity(int pathCount)
    {
        var capacity = GrowCapacity(pathCount);
        if (_styledPaths.Length < pathCount)
        {
            _styledPaths = new StyledPathInfo[capacity];
        }

        if (_pathSegmentCounts.Length < pathCount || _pathEmitOffsets.Length < pathCount)
        {
            _pathSegmentCounts = new int[capacity];
            _pathSegmentOffsets = new int[capacity];
            _pathEmitOffsets = new int[capacity];
        }
    }

    private void EnsureSegmentCapacity(int totalSegments)
    {
        var capacity = GrowCapacity(totalSegments);
        if (_segmentScratch.Length < totalSegments)
        {
            _segmentScratch = new StrokeSegment2D[capacity];
        }

        if (_silhouetteSegmentIndices.Length < totalSegments)
        {
            _silhouetteSegmentIndices = new int[capacity];
            _featureSegmentIndices = new int[capacity];
            _boundarySegmentIndices = new int[capacity];
        }
    }

    private static int GrowCapacity(int required)
    {
        var capacity = 4;
        while (capacity < required)
        {
            capacity = checked(capacity + (capacity >> 1));
        }

        return capacity;
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

    private int CountStyledPathSegments(
        in DefaultProjectedPath path,
        DefaultDrawingSettings drawing,
        int seed,
        string styleName,
        int passes,
        ref int emitOffset,
        InkPathScratch scratch)
    {
        var pointCount = path.Points.Count;
        if (pointCount < 2)
        {
            return 0;
        }

        var pathEmitOffset = emitOffset;
        var candidateCapacity = (pointCount - 1) * passes;
        EnsureEmitCapacity(emitOffset + candidateCapacity);

        var count = 0;
        scratch.EnsureCapacity(pointCount);
        var startPoints = scratch.StartPoints.AsSpan(0, pointCount);
        var endPoints = scratch.EndPoints.AsSpan(0, pointCount);

        for (var pass = 0; pass < passes; pass++)
        {
            var passStyle = StrokeHumanizationMath.Pass(styleName, path.Type == DefaultLineKind.Feature ? drawing.Jitter * 0.8f : drawing.Jitter, pass);
            var jitter = passStyle.Jitter;

            BuildJitteredPoints(path.Points, startPoints, jitter, seed + pass * 11, drawing.EnableFastNoise);
            BuildJitteredPoints(path.Points, endPoints, jitter, seed + pass * 11 + 3, drawing.EnableFastNoise);

            for (var i = 1; i < pointCount; i++)
            {
                var shouldKeep = !StrokeHumanizationMath.ShouldSkipSegment(styleName, i, seed, pass, drawing.EnableFastNoise);
                _segmentEmitFlags[pathEmitOffset] = shouldKeep ? (byte)1 : (byte)0;
                _pathPrecomputedStartPoints[pathEmitOffset] = startPoints[i - 1];
                _pathPrecomputedEndPoints[pathEmitOffset] = endPoints[i];
                if (shouldKeep)
                {
                    count++;
                }

                pathEmitOffset++;
            }
        }

        emitOffset = pathEmitOffset;
        return count;
    }

    private void EnsureEmitCapacity(int required)
    {
        if (_segmentEmitFlags.Length >= required)
        {
            return;
        }

        var capacity = GrowCapacity(required);
        Array.Resize(ref _segmentEmitFlags, capacity);
        Array.Resize(ref _pathPrecomputedStartPoints, capacity);
        Array.Resize(ref _pathPrecomputedEndPoints, capacity);
    }

    private int WriteStyledPathSegments(
        in StyledPathInfo info,
        Span<StrokeSegment2D> destination,
        int pathEmitOffset)
    {
        var path = info.Path;
        if (path.Points.Count < 2)
        {
            return 0;
        }

        var pointCount = path.Points.Count;
        var drawing = info.Drawing;
        var styleName = info.StyleName;
        var comic = info.StrokeStyle == DefaultStrokeStyle.ComicInk;
        var baseJitter = drawing.Jitter * (path.Type == DefaultLineKind.Feature ? 0.8f : 1f);
        var pressure = comic ? NumericMath.AtLeast((double)drawing.Pressure, 0.54d) : drawing.Pressure;
        var written = 0;
        var candidateOffset = pathEmitOffset;

        for (var pass = 0; pass < info.Passes; pass++)
        {
            var passStyle = StrokeHumanizationMath.Pass(styleName, baseJitter, pass);
            var alpha = passStyle.Alpha;
            var widthMultiplier = passStyle.WidthMultiplier;

            for (var i = 1; i < pointCount; i++)
            {
                if (_segmentEmitFlags[candidateOffset] == 0)
                {
                    candidateOffset++;
                    continue;
                }

                var t = i / (double)NumericMath.AtLeast(pointCount - 1, 1);
                var pressureNoise = StrokeHumanizationMath.PressureNoise(pressure, t, info.Seed, pass, comic, drawing.EnableFastNoise);
                var taper = StrokeHumanizationMath.Taper(t, comic);
                var lineWidth = StrokeHumanizationMath.LineWidth(info.LineStyle.BaseWidth, widthMultiplier, pressureNoise, taper);

                var segmentStyle = new StrokeStyle2D((float)lineWidth, NumericMath.Clamp01(alpha * info.LineStyle.BaseOpacity), info.LineStyle.StrokeColor);
                destination[written++] = new StrokeSegment2D(
                    _pathPrecomputedStartPoints[candidateOffset],
                    _pathPrecomputedEndPoints[candidateOffset],
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

                candidateOffset++;
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
        string StyleName,
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
