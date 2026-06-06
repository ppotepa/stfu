using System.Numerics;
using STFU.Common.Math;
using STFU.NPR.Graph;
using STFU.Parallelism;
using STFU.Strokes;

namespace STFU.NPR.Pipeline.Default.Steps;

public sealed class DefaultClassifyEdgesToFragmentsStep : STFU.NPR.Pipeline.INprStep
{
    private readonly List<EdgePartitionBuffer> _partitionBuffers = [];
    private int _lastFragmentCount;
    private int _lastDebugCurveCount;

    public void Execute(STFU.NPR.Pipeline.NprContext context)
    {
        var drawing = context.Settings.DefaultDrawing;
        var buffer = context.Graph.DefaultFaceIdVisibility;
        if (buffer is null)
        {
            return;
        }

        context.Graph.DefaultFragments.Clear();
        context.Graph.VisibilitySegments.Clear();
        context.Graph.Curves.Clear();
        context.Graph.FeatureLines.Clear();

        var featureThreshold = NumericMath.DegreesToRadians(drawing.FeatureAngleDegrees);
        var edgeCount = context.Graph.TopologyEdges.Count;
        var parallel = context.WorkerCount > 1 && edgeCount >= 512;
        var counters = new EdgeRangeCounters
        {
            SourceEdges = edgeCount,
            RangeCount = parallel ? DeterministicParallel.GetRangeCount(edgeCount, context.WorkerCount) : 1
        };

        if (!parallel)
        {
            var estimatedFragmentCapacity = EstimateFragmentCapacity(context);
            var estimatedDebugCurveCapacity = context.IncludeDebugFrame
                ? EstimateDebugCurveCapacity(estimatedFragmentCapacity)
                : 0;
            var includeVisibilityOutputs = context.IncludeDebugFrame;

            context.Graph.DefaultFragments.EnsureCapacity(estimatedFragmentCapacity);
            if (includeVisibilityOutputs)
            {
                context.Graph.VisibilitySegments.EnsureCapacity(estimatedFragmentCapacity);
                context.Graph.Curves.EnsureCapacity(estimatedDebugCurveCapacity);
                context.Graph.FeatureLines.EnsureCapacity(estimatedDebugCurveCapacity);
            }

            ProcessRange(
                context,
                buffer,
                0,
                edgeCount,
                featureThreshold,
                context.Graph.DefaultFragments,
                includeVisibilityOutputs ? context.Graph.VisibilitySegments : null,
                context.IncludeDebugFrame ? context.Graph.Curves : null,
                ref counters);
        }
        else
        {
            var rangeCount = DeterministicParallel.GetRangeCount(edgeCount, context.WorkerCount);
            var partitionSize = (edgeCount + rangeCount - 1) / rangeCount;
            var includeVisibilityOutputs = context.IncludeDebugFrame;
            var partitions = RentPartitionBuffers(rangeCount, context.IncludeDebugFrame, partitionSize);

            DeterministicParallel.ForRanges(
                0,
                edgeCount,
                context.WorkerCount,
                context.CancellationToken,
                (start, end, partitionIndex, cancellationToken) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var partition = partitions[partitionIndex];
                    partition.Reset(context.IncludeDebugFrame, partition.GetSuggestedCapacity(end - start));
                    partition.Counters = new EdgeRangeCounters();
                    ProcessRange(
                        context,
                        buffer,
                        start,
                        end,
                        featureThreshold,
                        partition.Fragments,
                        includeVisibilityOutputs ? partition.VisibilitySegments : null,
                        partition.Curves,
                        ref partition.Counters);
                });

            var totalFragmentCount = 0;
            var totalCurveCount = 0;
            var totalVisibilityCount = 0;
            for (var partitionIndex = 0; partitionIndex < partitions.Length; partitionIndex++)
            {
                var partition = partitions[partitionIndex];
                totalFragmentCount += partition.Fragments.Count;
                totalVisibilityCount += partition.VisibilitySegments.Count;
                totalCurveCount += partition.Curves?.Count ?? 0;
                counters.Add(partition.Counters);
            }

            context.Graph.DefaultFragments.EnsureCapacity(totalFragmentCount);
            if (includeVisibilityOutputs)
            {
                context.Graph.VisibilitySegments.EnsureCapacity(totalVisibilityCount);
                context.Graph.Curves.EnsureCapacity(totalCurveCount);
                context.Graph.FeatureLines.EnsureCapacity(totalCurveCount);
            }

            for (var partitionIndex = 0; partitionIndex < partitions.Length; partitionIndex++)
            {
                var partition = partitions[partitionIndex];
                context.Graph.DefaultFragments.AddRange(partition.Fragments);
                if (includeVisibilityOutputs)
                {
                    context.Graph.VisibilitySegments.AddRange(partition.VisibilitySegments);
                }

                if (partition.Curves is { } curves)
                {
                    for (var i = 0; i < curves.Count; i++)
                    {
                        context.Graph.AddCurve(curves[i]);
                    }
                }
            }
        }

        _lastFragmentCount = context.Graph.DefaultFragments.Count;
        if (context.IncludeDebugFrame)
        {
            _lastDebugCurveCount = context.Graph.Curves.Count;
        }

        context.Counters.Set("DefaultClassifyEdgesToFragmentsStep.sourceEdges", counters.SourceEdges);
        context.Counters.Set("DefaultClassifyEdgesToFragmentsStep.rangeCount", counters.RangeCount);
        context.Counters.Set("DefaultClassifyEdgesToFragmentsStep.edgesAfterStride", counters.EdgesAfterStride);
        context.Counters.Set("DefaultClassifyEdgesToFragmentsStep.edgesClassified", counters.EdgesClassified);
        context.Counters.Set("DefaultClassifyEdgesToFragmentsStep.visibilitySamples", counters.VisibilitySamples);
        context.Counters.Set("DefaultClassifyEdgesToFragmentsStep.fragmentsEmitted", counters.FragmentsEmitted);
        context.Counters.Set("DefaultClassifyEdgesToFragmentsStep.debugCurvesEmitted", counters.DebugCurvesEmitted);
    }

    private int EstimateFragmentCapacity(STFU.NPR.Pipeline.NprContext context)
    {
        return NumericMath.AtLeast(context.Graph.TopologyEdges.Count, _lastFragmentCount);
    }

    private int EstimateDebugCurveCapacity(int estimatedFragmentCapacity)
    {
        return NumericMath.AtLeast(estimatedFragmentCapacity, _lastDebugCurveCount);
    }

    private EdgePartitionBuffer[] RentPartitionBuffers(int workerCount, bool includeDebugFrame, int partitionSize)
    {
        while (_partitionBuffers.Count < workerCount)
        {
            _partitionBuffers.Add(new EdgePartitionBuffer());
        }

        var result = new EdgePartitionBuffer[workerCount];
        for (var i = 0; i < workerCount; i++)
        {
            var buffer = _partitionBuffers[i];
            buffer.Reset(includeDebugFrame, buffer.GetSuggestedCapacity(partitionSize));
            result[i] = buffer;
        }

        return result;
    }

    private static void ProcessRange(
        STFU.NPR.Pipeline.NprContext context,
        DefaultFaceIdVisibilityBuffer buffer,
        int startEdgeIndex,
        int endEdgeIndex,
        float featureThreshold,
        List<DefaultLineFragment> fragments,
        List<VisibilitySegment>? visibilitySegments,
        List<FeatureCurve>? curves,
        ref EdgeRangeCounters counters)
    {
        var drawing = context.Settings.DefaultDrawing;
        var graph = context.Graph;
        var edges = graph.TopologyEdges;
        var vertices = graph.Vertices;
        var triangles = graph.Triangles;
        var faceVisible = buffer.FaceVisible;
        var occlusionCulling = drawing.OcclusionCulling;
        for (var edgeIndex = startEdgeIndex; edgeIndex < endEdgeIndex; edgeIndex++)
        {
            var edge = edges[edgeIndex];
            var strideCounter = edgeIndex + 1;
            if (drawing.MeshStride > 1 && strideCounter % drawing.MeshStride != 0)
            {
                continue;
            }

            counters.EdgesAfterStride++;
            var vis0 = IsFaceVisibleFast(triangles, faceVisible, edge.FirstTriangleIndex, occlusionCulling);
            var vis1 = IsFaceVisibleFast(triangles, faceVisible, edge.SecondTriangleIndex, occlusionCulling);
            var front0 = IsFrontFacing(triangles, edge.FirstTriangleIndex);
            var front1 = IsFrontFacing(triangles, edge.SecondTriangleIndex);

            if (!TryClassifyEdge(edge, drawing, vis0, vis1, front0, front1, featureThreshold, out var lineKind, out var curveKind, out var intent))
            {
                continue;
            }

            counters.EdgesClassified++;
            var start = vertices[edge.StartVertexIndex];
            var end = vertices[edge.EndVertexIndex];

            if (drawing.CullOutside && NdcOutsideSameSide(start.Ndc, end.Ndc))
            {
                continue;
            }

            var length = DefaultPointPathAdapter.SegmentLength(start.Position, end.Position);
            if (drawing.MinSegPx > 0f && length < drawing.MinSegPx)
            {
                continue;
            }

            AppendVisibleFragmentsForEdge(
                fragments,
                visibilitySegments,
                curves,
                context,
                buffer,
                edge,
                lineKind,
                curveKind,
                intent,
                start,
                end,
                length,
                ref counters);
        }
    }

    private static bool TryClassifyEdge(
        TopologyEdge edge,
        STFU.NPR.Settings.DefaultDrawingSettings drawing,
        bool vis0,
        bool vis1,
        bool front0,
        bool front1,
        float featureThresholdRadians,
        out DefaultLineKind lineKind,
        out FeatureCurveKind curveKind,
        out NprStrokeIntent intent)
    {
        lineKind = default;
        curveKind = default;
        intent = default;

        if (edge.IsBoundary)
        {
            if (drawing.ShowBoundary && vis0)
            {
                lineKind = DefaultLineKind.Boundary;
                curveKind = FeatureCurveKind.Boundary;
                intent = NprStrokeIntent.Boundary;
                return true;
            }

            return false;
        }

        if (drawing.ShowSilhouette && ((front0 && !front1) || (!front0 && front1)) && (vis0 || vis1))
        {
            lineKind = DefaultLineKind.Silhouette;
            curveKind = FeatureCurveKind.Silhouette;
            intent = NprStrokeIntent.Silhouette;
            return true;
        }

        if (drawing.ShowFeature && vis0 && vis1)
        {
            var angle = NumericMath.DegreesToRadians(edge.NormalAngleDegrees);
            if (angle >= featureThresholdRadians)
            {
                lineKind = DefaultLineKind.Feature;
                curveKind = FeatureCurveKind.Crease;
                intent = NprStrokeIntent.Crease;
                return true;
            }
        }

        return false;
    }

    private static void AppendVisibleFragmentsForEdge(
        List<DefaultLineFragment> fragments,
        List<VisibilitySegment>? visibilitySegments,
        List<FeatureCurve>? curves,
        STFU.NPR.Pipeline.NprContext context,
        DefaultFaceIdVisibilityBuffer buffer,
        TopologyEdge edge,
        DefaultLineKind lineKind,
        FeatureCurveKind curveKind,
        NprStrokeIntent intent,
        ProjectedVertex start,
        ProjectedVertex end,
        float length,
        ref EdgeRangeCounters counters)
    {
        var drawing = context.Settings.DefaultDrawing;
        var minSegmentLength = NumericMath.AtLeast(drawing.MinSegPx, 0.5f);
        if (length < minSegmentLength)
        {
            return;
        }

        if (!drawing.OcclusionCulling)
        {
            if (AppendFragment(
                fragments,
                visibilitySegments,
                curves,
                context,
                edge,
                lineKind,
                curveKind,
                intent,
                start.Position,
                end.Position,
                0f,
                1f,
                0))
            {
                counters.FragmentsEmitted++;
                if (curves is not null)
                {
                    counters.DebugCurvesEmitted++;
                }
            }

            return;
        }

        var firstAllowedFace = edge.FirstTriangleIndex;
        var secondAllowedFace = edge.SecondTriangleIndex;
        if (firstAllowedFace < 0 && secondAllowedFace < 0)
        {
            return;
        }

        var scaleX = buffer.Width / (float)NumericMath.AtLeast(context.Width, 1);
        var scaleY = buffer.Height / (float)NumericMath.AtLeast(context.Height, 1);
        var maxX = buffer.Width - 1;
        var maxY = buffer.Height - 1;
        var startVisible = TrySampleEdgeVisibility(
            buffer,
            start,
            end,
            firstAllowedFace,
            secondAllowedFace,
            scaleX,
            scaleY,
            maxX,
            maxY,
            0f,
            out _);
        counters.VisibilitySamples++;
        var midVisible = TrySampleEdgeVisibility(
            buffer,
            start,
            end,
            firstAllowedFace,
            secondAllowedFace,
            scaleX,
            scaleY,
            maxX,
            maxY,
            0.5f,
            out _);
        counters.VisibilitySamples++;
        var endVisible = TrySampleEdgeVisibility(
            buffer,
            start,
            end,
            firstAllowedFace,
            secondAllowedFace,
            scaleX,
            scaleY,
            maxX,
            maxY,
            1f,
            out _);
        counters.VisibilitySamples++;
        if (startVisible && midVisible && endVisible)
        {
            if (AppendFragment(
                fragments,
                visibilitySegments,
                curves,
                context,
                edge,
                lineKind,
                curveKind,
                intent,
                start.Position,
                end.Position,
                0f,
                1f,
                0))
            {
                counters.FragmentsEmitted++;
                if (curves is not null)
                {
                    counters.DebugCurvesEmitted++;
                }
            }

            return;
        }

        if (length <= minSegmentLength * 2f && !startVisible && !midVisible && !endVisible)
        {
            return;
        }

        var samples = NumericMath.AtMost(96, NumericMath.AtLeast((int)NumericMath.Ceiling(length / 4f), 3));
        var runStart = default(Point2D);
        var previousPoint = default(Point2D);
        var runStartT = 0f;
        var previousT = 0f;
        var previousVisible = false;
        var hasRunStart = false;
        var hasPreviousPoint = false;
        var fragmentIndex = 0;

        for (var i = 0; i <= samples; i++)
        {
            var t = i / (float)samples;
            var visible = TrySampleEdgeVisibility(
                buffer,
                start,
                end,
                firstAllowedFace,
                secondAllowedFace,
                scaleX,
                scaleY,
                maxX,
                maxY,
                t,
                out var point);
            counters.VisibilitySamples++;

            if (visible && !previousVisible)
            {
                runStart = point;
                runStartT = t;
                hasRunStart = true;
            }

            if (!visible && previousVisible && hasRunStart && hasPreviousPoint)
            {
                if (DefaultPointPathAdapter.SegmentLength(runStart, previousPoint) >= minSegmentLength)
                {
                    if (AppendFragment(
                        fragments,
                        visibilitySegments,
                        curves,
                        context,
                        edge,
                        lineKind,
                        curveKind,
                        intent,
                        runStart,
                        previousPoint,
                        runStartT,
                        previousT,
                        fragmentIndex++))
                    {
                        counters.FragmentsEmitted++;
                        if (curves is not null)
                        {
                            counters.DebugCurvesEmitted++;
                        }
                    }
                }

                hasRunStart = false;
            }

            previousVisible = visible;
            previousPoint = point;
            previousT = t;
            hasPreviousPoint = true;
        }

        if (previousVisible && hasRunStart && hasPreviousPoint &&
            DefaultPointPathAdapter.SegmentLength(runStart, previousPoint) >= minSegmentLength)
        {
            if (AppendFragment(
                fragments,
                visibilitySegments,
                curves,
                context,
                edge,
                lineKind,
                curveKind,
                intent,
                runStart,
                previousPoint,
                runStartT,
                previousT,
                fragmentIndex))
            {
                counters.FragmentsEmitted++;
                if (curves is not null)
                {
                    counters.DebugCurvesEmitted++;
                }
            }
        }
    }

    private static bool TrySampleEdgeVisibility(
        DefaultFaceIdVisibilityBuffer buffer,
        ProjectedVertex start,
        ProjectedVertex end,
        int firstAllowedFace,
        int secondAllowedFace,
        float scaleX,
        float scaleY,
        int maxX,
        int maxY,
        float t,
        out Point2D point)
    {
        var interpolated = Geometry2D.LerpPoint(
            start.Position.X,
            start.Position.Y,
            end.Position.X,
            end.Position.Y,
            t);
        point = new Point2D(interpolated.X, interpolated.Y);
        var ndc = Vector3.Lerp(start.Ndc, end.Ndc, t);
        var inClip = !(ndc.X < -1f || ndc.X > 1f || ndc.Y < -1f || ndc.Y > 1f || ndc.Z < -1f || ndc.Z > 1f);
        if (!inClip)
        {
            return false;
        }

        var cx = NumericMath.Clamp((int)NumericMath.Floor(point.X * scaleX), 0, maxX);
        var cy = NumericMath.Clamp((int)NumericMath.Floor(point.Y * scaleY), 0, maxY);
        return buffer.SampleOwnedFaceAtBuffer(
            cx,
            cy,
            firstAllowedFace,
            secondAllowedFace);
    }

    private static bool AppendFragment(
        List<DefaultLineFragment> fragments,
        List<VisibilitySegment>? visibilitySegments,
        List<FeatureCurve>? curves,
        STFU.NPR.Pipeline.NprContext context,
        TopologyEdge edge,
        DefaultLineKind lineKind,
        FeatureCurveKind curveKind,
        NprStrokeIntent intent,
        Point2D p0,
        Point2D p1,
        float t0,
        float t1,
        int fragmentIndex)
    {
        if (DefaultPointPathAdapter.SegmentLength(p0, p1) <= 0.5f)
        {
            return false;
        }

        var a = context.Graph.Vertices[edge.StartVertexIndex];
        var b = context.Graph.Vertices[edge.EndVertexIndex];
        var depth = (a.Depth + b.Depth) * 0.5f;
        var stableId = HashMath.StableTriple(edge.StableId, fragmentIndex, (int)lineKind);
        fragments.Add(new DefaultLineFragment(
            stableId,
            lineKind,
            p0,
            p1,
            edge.StableId,
            edge.FirstTriangleIndex,
            edge.SecondTriangleIndex,
            t0,
            t1,
            depth));

        var importance = lineKind == DefaultLineKind.Silhouette ? 1f : 0.7f;
        var entityId = context.Graph.Triangles[NumericMath.AtLeast(edge.FirstTriangleIndex, 0)].EntityId;

        visibilitySegments?.Add(new VisibilitySegment(
            stableId,
            edge.StableId,
            curveKind,
            intent,
            VisibilityState.Visible,
            t0,
            t1,
            p0,
            p1,
            depth,
            0f,
            importance,
            1f));

        if (curves is not null)
        {
            var flags = lineKind == DefaultLineKind.Silhouette ? FeatureCurveFlags.ViewDependent : FeatureCurveFlags.None;
            curves.Add(FeatureCurve.FromLine(
                stableId,
                curveKind,
                intent,
                new FeaturePoint(p0, depth),
                new FeaturePoint(p1, depth),
                new FeatureCurveSource(edge.StartVertexIndex, edge.EndVertexIndex, edge.FirstTriangleIndex, edge.SecondTriangleIndex),
                0f,
                importance,
                1f,
                flags,
                entityId: entityId));
        }

        return true;
    }

    private static bool IsFaceVisibleFast(List<ProjectedTriangle> triangles, bool[] faceVisible, int triangleIndex, bool occlusionCulling)
    {
        return triangleIndex >= 0 &&
               triangleIndex < triangles.Count &&
               (!occlusionCulling || faceVisible[triangleIndex]) &&
               triangles[triangleIndex].IsFrontFacing;
    }

    private static bool IsFrontFacing(List<ProjectedTriangle> triangles, int triangleIndex)
    {
        return triangleIndex >= 0 &&
               triangleIndex < triangles.Count &&
               triangles[triangleIndex].IsFrontFacing;
    }

    private static bool NdcOutsideSameSide(Vector3 a, Vector3 b)
    {
        return (a.X < -1f && b.X < -1f) ||
               (a.X > 1f && b.X > 1f) ||
               (a.Y < -1f && b.Y < -1f) ||
               (a.Y > 1f && b.Y > 1f);
    }

    private sealed class EdgePartitionBuffer
    {
        private int _lastFragmentCount;
        private int _lastCurveCount;

        public EdgeRangeCounters Counters;

        public List<DefaultLineFragment> Fragments { get; } = [];
        public List<VisibilitySegment> VisibilitySegments { get; } = [];
        public List<FeatureCurve>? Curves { get; private set; }

        public int GetSuggestedCapacity(int partitionSize)
        {
            return NumericMath.AtLeast(partitionSize, _lastFragmentCount);
        }

        public void Reset(bool includeDebugFrame, int suggestedCapacity)
        {
            _lastFragmentCount = Fragments.Count;
            _lastCurveCount = Curves?.Count ?? 0;
            Fragments.Clear();
            VisibilitySegments.Clear();
            Fragments.EnsureCapacity(suggestedCapacity);
            VisibilitySegments.EnsureCapacity(suggestedCapacity);

            if (includeDebugFrame)
            {
                Curves ??= [];
                Curves.Clear();
                Curves.EnsureCapacity(NumericMath.AtLeast(suggestedCapacity, _lastCurveCount));
            }
            else
            {
                Curves = null;
            }
        }
    }

    private struct EdgeRangeCounters
    {
        public long SourceEdges;
        public long RangeCount;
        public long EdgesAfterStride;
        public long EdgesClassified;
        public long VisibilitySamples;
        public long FragmentsEmitted;
        public long DebugCurvesEmitted;

        public void Add(EdgeRangeCounters other)
        {
            EdgesAfterStride += other.EdgesAfterStride;
            EdgesClassified += other.EdgesClassified;
            VisibilitySamples += other.VisibilitySamples;
            FragmentsEmitted += other.FragmentsEmitted;
            DebugCurvesEmitted += other.DebugCurvesEmitted;
        }
    }
}
