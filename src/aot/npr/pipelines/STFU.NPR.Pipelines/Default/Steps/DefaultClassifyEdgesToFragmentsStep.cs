using System.Numerics;
using STFU.NPR.Graph;
using STFU.Strokes;

namespace STFU.NPR.Pipeline.Default.Steps;

public sealed class DefaultClassifyEdgesToFragmentsStep : STFU.NPR.Pipeline.INprStep
{
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

        var visibleFaces = ComputeVisibleFaces(context, buffer);
        var featureThreshold = DegreesToRadians(drawing.FeatureAngleDegrees);
        var strideCounter = 0;

        foreach (var edge in context.Graph.TopologyEdges)
        {
            strideCounter++;
            if (drawing.MeshStride > 1 && strideCounter % drawing.MeshStride != 0)
            {
                continue;
            }

            var vis0 = IsFaceVisible(visibleFaces, edge.FirstTriangleIndex);
            var vis1 = IsFaceVisible(visibleFaces, edge.SecondTriangleIndex);
            var front0 = IsFrontFacing(context, edge.FirstTriangleIndex);
            var front1 = IsFrontFacing(context, edge.SecondTriangleIndex);

            if (!TryClassifyEdge(edge, drawing, vis0, vis1, front0, front1, featureThreshold, out var lineKind, out var curveKind, out var intent))
            {
                continue;
            }

            var start = context.Graph.Vertices[edge.StartVertexIndex];
            var end = context.Graph.Vertices[edge.EndVertexIndex];

            if (drawing.CullOutside && NdcOutsideSameSide(start.Ndc, end.Ndc))
            {
                continue;
            }

            var length = DefaultPathMath.SegmentLength(start.Position, end.Position);
            if (drawing.MinSegPx > 0f && length < drawing.MinSegPx)
            {
                continue;
            }

            var fragments = VisibleFragmentsForEdge(context, buffer, edge, lineKind);
            if (fragments.Count == 0)
            {
                continue;
            }

            foreach (var fragment in fragments)
            {
                context.Graph.DefaultFragments.Add(fragment);

                var visibility = new VisibilitySegment(
                    fragment.StableId,
                    fragment.EdgeStableId,
                    curveKind,
                    intent,
                    VisibilityState.Visible,
                    fragment.StartT,
                    fragment.EndT,
                    fragment.P0,
                    fragment.P1,
                    fragment.Depth,
                    0f,
                    lineKind == DefaultLineKind.Silhouette ? 1f : 0.7f,
                    1f,
                    null,
                    context.Graph.Triangles[Math.Max(0, edge.FirstTriangleIndex)].EntityId);
                context.Graph.VisibilitySegments.Add(visibility);

                var curve = FeatureCurve.FromLine(
                    fragment.StableId,
                    curveKind,
                    intent,
                    new FeaturePoint(fragment.P0, fragment.Depth),
                    new FeaturePoint(fragment.P1, fragment.Depth),
                    new FeatureCurveSource(
                        edge.StartVertexIndex,
                        edge.EndVertexIndex,
                        edge.FirstTriangleIndex,
                        edge.SecondTriangleIndex),
                    0f,
                    lineKind == DefaultLineKind.Silhouette ? 1f : 0.7f,
                    1f,
                    lineKind == DefaultLineKind.Silhouette ? FeatureCurveFlags.ViewDependent : FeatureCurveFlags.None,
                    entityId: context.Graph.Triangles[Math.Max(0, edge.FirstTriangleIndex)].EntityId);

                context.Graph.AddCurve(curve);
            }
        }
    }

    private static bool[] ComputeVisibleFaces(STFU.NPR.Pipeline.NprContext context, DefaultFaceIdVisibilityBuffer buffer)
    {
        var visible = new bool[context.Graph.Triangles.Count];
        for (var i = 0; i < context.Graph.Triangles.Count; i++)
        {
            var triangle = context.Graph.Triangles[i];
            visible[i] = (!context.Settings.DefaultDrawing.OcclusionCulling || buffer.FaceVisible[i]) && triangle.IsFrontFacing;
        }

        return visible;
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
            var angle = DegreesToRadians(edge.NormalAngleDegrees);
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

    private static IReadOnlyList<DefaultLineFragment> VisibleFragmentsForEdge(
        STFU.NPR.Pipeline.NprContext context,
        DefaultFaceIdVisibilityBuffer buffer,
        TopologyEdge edge,
        DefaultLineKind lineKind)
    {
        var drawing = context.Settings.DefaultDrawing;
        var start = context.Graph.Vertices[edge.StartVertexIndex];
        var end = context.Graph.Vertices[edge.EndVertexIndex];
        var length = DefaultPathMath.SegmentLength(start.Position, end.Position);
        if (length < Math.Max(0.5f, drawing.MinSegPx))
        {
            return [];
        }

        if (!drawing.OcclusionCulling)
        {
            return [CreateFragment(context, edge, lineKind, start.Position, end.Position, 0f, 1f, 0)];
        }

        var allowedFaces = EdgeAllowedFaces(edge);
        if (allowedFaces.Count == 0)
        {
            return [];
        }

        var samples = Math.Min(96, Math.Max(7, (int)MathF.Ceiling(length / 4f)));
        var fragments = new List<DefaultLineFragment>();
        Point2D? runStart = null;
        Point2D? previousPoint = null;
        var runStartT = 0f;
        var previousT = 0f;
        var previousVisible = false;
        var fragmentIndex = 0;

        for (var i = 0; i <= samples; i++)
        {
            var t = i / (float)samples;
            var point = Lerp(start.Position, end.Position, t);
            var ndc = Vector3.Lerp(start.Ndc, end.Ndc, t);
            var inClip = !(ndc.X < -1f || ndc.X > 1f || ndc.Y < -1f || ndc.Y > 1f || ndc.Z < -1f || ndc.Z > 1f);
            var visible = inClip && buffer.SampleOwnedFaceAtScreen(point.X, point.Y, context.Width, context.Height, allowedFaces);

            if (visible && !previousVisible)
            {
                runStart = point;
                runStartT = t;
            }

            if (!visible && previousVisible && runStart is not null && previousPoint is not null)
            {
                if (DefaultPathMath.SegmentLength(runStart.Value, previousPoint.Value) >= Math.Max(0.5f, drawing.MinSegPx))
                {
                    fragments.Add(CreateFragment(context, edge, lineKind, runStart.Value, previousPoint.Value, runStartT, previousT, fragmentIndex++));
                }

                runStart = null;
            }

            previousVisible = visible;
            previousPoint = point;
            previousT = t;
        }

        if (previousVisible && runStart is not null && previousPoint is not null &&
            DefaultPathMath.SegmentLength(runStart.Value, previousPoint.Value) >= Math.Max(0.5f, drawing.MinSegPx))
        {
            fragments.Add(CreateFragment(context, edge, lineKind, runStart.Value, previousPoint.Value, runStartT, previousT, fragmentIndex));
        }

        return fragments;
    }

    private static DefaultLineFragment CreateFragment(
        STFU.NPR.Pipeline.NprContext context,
        TopologyEdge edge,
        DefaultLineKind lineKind,
        Point2D start,
        Point2D end,
        float startT,
        float endT,
        int fragmentIndex)
    {
        var a = context.Graph.Vertices[edge.StartVertexIndex];
        var b = context.Graph.Vertices[edge.EndVertexIndex];
        var depth = (a.Depth + b.Depth) * 0.5f;

        unchecked
        {
            var stableId = edge.StableId * 397 ^ fragmentIndex * 17 ^ (int)lineKind;
            return new DefaultLineFragment(
                stableId,
                lineKind,
                start,
                end,
                edge.StableId,
                edge.FirstTriangleIndex,
                edge.SecondTriangleIndex,
                startT,
                endT,
                depth);
        }
    }

    private static HashSet<int> EdgeAllowedFaces(TopologyEdge edge)
    {
        var allowedFaces = new HashSet<int>();
        if (edge.FirstTriangleIndex >= 0)
        {
            allowedFaces.Add(edge.FirstTriangleIndex);
        }

        if (edge.SecondTriangleIndex >= 0)
        {
            allowedFaces.Add(edge.SecondTriangleIndex);
        }

        return allowedFaces;
    }

    private static bool IsFaceVisible(bool[] visibleFaces, int index)
    {
        return (uint)index < (uint)visibleFaces.Length && visibleFaces[index];
    }

    private static bool IsFrontFacing(STFU.NPR.Pipeline.NprContext context, int triangleIndex)
    {
        return (uint)triangleIndex < (uint)context.Graph.Triangles.Count &&
            context.Graph.Triangles[triangleIndex].IsFrontFacing;
    }

    private static bool NdcOutsideSameSide(Vector3 a, Vector3 b)
    {
        if (a.X < -1f && b.X < -1f) return true;
        if (a.X > 1f && b.X > 1f) return true;
        if (a.Y < -1f && b.Y < -1f) return true;
        if (a.Y > 1f && b.Y > 1f) return true;
        return false;
    }

    private static Point2D Lerp(Point2D a, Point2D b, float t)
    {
        return new Point2D(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
    }

    private static float DegreesToRadians(float degrees)
    {
        return degrees * MathF.PI / 180f;
    }
}
