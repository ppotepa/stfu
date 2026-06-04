using System.Numerics;
using STFU.NPR.Graph;
using STFU.NPR.Pipeline;

namespace STFU.NPR.Steps.Mesh;

public sealed class ExtractFeatureLinesStep : INprStep
{
    private const float RidgeValleyNormalAngleFloorDegrees = 6f;
    private const float RidgeShadeThreshold = 0.58f;
    private const float ValleyShadeThreshold = 0.42f;
    private const float ApparentRidgeViewDotMax = 0.28f;
    private const float SuggestiveContourViewDotMax = 0.34f;
    private const float SuggestiveContourViewDotMin = 0.08f;

    public void Execute(NprContext context)
    {
        context.Graph.Curves.Clear();
        context.Graph.FeatureLines.Clear();

        foreach (var edge in context.Graph.TopologyEdges)
        {
            if (!TryGetVisibleVertices(context.Graph, edge, out var start, out var end))
            {
                continue;
            }

            if (!TryClassify(context, edge, context.Settings.CreaseAngleDegrees, out var kind, out var intent, out var curvatureFactor, out var confidence))
            {
                continue;
            }

            var shade = CalculateShade(context.Graph, edge);
            var angleFactor = Math.Clamp(edge.NormalAngleDegrees / 90f, 0f, 1f);
            var depth = (start.Depth + end.Depth) * 0.5f;
            var importance = kind switch
            {
                FeatureCurveKind.Silhouette => 1.0f,
                FeatureCurveKind.Boundary => 0.9f,
                FeatureCurveKind.Crease => 0.55f + angleFactor * 0.35f + shade * 0.08f,
                FeatureCurveKind.ApparentRidge => 0.58f + curvatureFactor * 0.30f + angleFactor * 0.12f,
                FeatureCurveKind.Ridge => 0.46f + curvatureFactor * 0.34f + shade * 0.16f,
                FeatureCurveKind.Valley => 0.44f + curvatureFactor * 0.34f + (1f - shade) * 0.16f,
                FeatureCurveKind.SuggestiveContour => 0.52f + curvatureFactor * 0.24f + angleFactor * 0.14f,
                _ => 0.35f
            };

            context.Graph.AddCurve(FeatureCurve.FromLine(
                edge.StableId,
                kind,
                intent,
                new FeaturePoint(start.Position, start.Depth),
                new FeaturePoint(end.Position, end.Depth),
                new FeatureCurveSource(
                    edge.StartVertexIndex,
                    edge.EndVertexIndex,
                    edge.FirstTriangleIndex,
                    edge.SecondTriangleIndex),
                shade,
                Math.Clamp(importance, 0f, 1f),
                confidence,
                kind is FeatureCurveKind.Silhouette or FeatureCurveKind.SuggestiveContour or FeatureCurveKind.ApparentRidge
                    ? FeatureCurveFlags.ViewDependent
                    : FeatureCurveFlags.None));
        }
    }

    private static bool TryClassify(
        NprContext context,
        TopologyEdge edge,
        float creaseAngleDegrees,
        out FeatureCurveKind kind,
        out NprStrokeIntent intent,
        out float curvatureFactor,
        out float confidence)
    {
        var graph = context.Graph;
        curvatureFactor = EstimateCurvature(graph, edge);
        confidence = 1f;

        if (edge.IsBoundary)
        {
            kind = FeatureCurveKind.Boundary;
            intent = NprStrokeIntent.Boundary;
            confidence = 1f;
            return true;
        }

        var first = graph.Triangles[edge.FirstTriangleIndex];
        var second = graph.Triangles[edge.SecondTriangleIndex];

        if (first.IsFrontFacing != second.IsFrontFacing)
        {
            kind = FeatureCurveKind.Silhouette;
            intent = NprStrokeIntent.Silhouette;
            confidence = 1f;
            return true;
        }

        if (edge.NormalAngleDegrees >= creaseAngleDegrees && first.IsFrontFacing && second.IsFrontFacing)
        {
            kind = FeatureCurveKind.Crease;
            intent = NprStrokeIntent.Crease;
            confidence = Math.Clamp((edge.NormalAngleDegrees - creaseAngleDegrees) / MathF.Max(8f, 90f - creaseAngleDegrees), 0.65f, 1f);
            return true;
        }

        if (TryClassifyMaterialBoundary(context.Graph, edge, first, second, out kind, out confidence))
        {
            intent = NprStrokeIntent.Accent;
            return true;
        }

        if (TryClassifyApparentRidge(context, edge, first, second, curvatureFactor, out kind, out confidence) ||
            TryClassifySuggestiveContour(context, edge, first, second, curvatureFactor, out kind, out confidence) ||
            TryClassifyCurvatureFeature(graph, edge, first, second, curvatureFactor, out kind, out confidence))
        {
            intent = NprStrokeIntent.Accent;
            return true;
        }

        kind = FeatureCurveKind.Accent;
        intent = NprStrokeIntent.Accent;
        confidence = 0f;
        return false;
    }

    private static bool TryClassifyMaterialBoundary(
        NprGraph graph,
        TopologyEdge edge,
        ProjectedTriangle first,
        ProjectedTriangle second,
        out FeatureCurveKind kind,
        out float confidence)
    {
        kind = FeatureCurveKind.Accent;
        confidence = 0f;

        if (!first.IsFrontFacing || !second.IsFrontFacing)
        {
            return false;
        }

        var firstRegion = FindRegion(graph.MaterialRegions, edge.FirstTriangleIndex);
        var secondRegion = FindRegion(graph.MaterialRegions, edge.SecondTriangleIndex);
        if (firstRegion is null || secondRegion is null || firstRegion.StableId == secondRegion.StableId)
        {
            return false;
        }

        if (firstRegion.MaterialId == secondRegion.MaterialId)
        {
            return false;
        }

        var toneDelta = MathF.Abs(firstRegion.BaseTone - secondRegion.BaseTone);
        if (toneDelta < 0.12f)
        {
            return false;
        }

        kind = FeatureCurveKind.MaterialBoundary;
        confidence = Math.Clamp(
            toneDelta * 0.65f +
            MathF.Abs(first.Shade - second.Shade) * 0.2f +
            Math.Clamp(edge.NormalAngleDegrees / 24f, 0f, 1f) * 0.15f,
            0f,
            1f);
        return true;
    }

    private static bool TryClassifySuggestiveContour(
        NprContext context,
        TopologyEdge edge,
        ProjectedTriangle first,
        ProjectedTriangle second,
        float curvatureFactor,
        out FeatureCurveKind kind,
        out float confidence)
    {
        kind = FeatureCurveKind.Accent;
        confidence = 0f;

        if (!first.IsFrontFacing || !second.IsFrontFacing)
        {
            return false;
        }

        if (edge.NormalAngleDegrees >= MathF.Max(14f, context.Settings.CreaseAngleDegrees * 0.45f))
        {
            return false;
        }

        if (curvatureFactor < 0.05f)
        {
            return false;
        }

        var viewForward = context.View.Projection.Forward;
        var start = context.Graph.Vertices[edge.StartVertexIndex];
        var end = context.Graph.Vertices[edge.EndVertexIndex];
        var startDot = MathF.Abs(Vector3.Dot(NormalizeOrDefault(start.WorldNormal), -viewForward));
        var endDot = MathF.Abs(Vector3.Dot(NormalizeOrDefault(end.WorldNormal), -viewForward));
        var nearContour = (startDot + endDot) * 0.5f;
        if (nearContour > SuggestiveContourViewDotMax || nearContour < SuggestiveContourViewDotMin)
        {
            return false;
        }

        var dotSpread = MathF.Abs(startDot - endDot);
        var shade = (first.Shade + second.Shade) * 0.5f;
        if (dotSpread < 0.04f || shade < 0.35f || shade > 0.92f)
        {
            return false;
        }

        var radialSignal = EstimateRadialSignal(context.Graph, edge, -viewForward);
        if (radialSignal < 0.08f)
        {
            return false;
        }

        kind = FeatureCurveKind.SuggestiveContour;
        confidence = Math.Clamp(
            (1f - MathF.Abs(nearContour - 0.2f) / 0.16f) * 0.45f +
            Math.Clamp(dotSpread / 0.18f, 0f, 1f) * 0.25f +
            Math.Clamp(curvatureFactor / 0.22f, 0f, 1f) * 0.15f +
            Math.Clamp(radialSignal / 0.28f, 0f, 1f) * 0.15f,
            0f,
            1f);
        return true;
    }

    private static bool TryClassifyApparentRidge(
        NprContext context,
        TopologyEdge edge,
        ProjectedTriangle first,
        ProjectedTriangle second,
        float curvatureFactor,
        out FeatureCurveKind kind,
        out float confidence)
    {
        kind = FeatureCurveKind.Accent;
        confidence = 0f;

        if (!first.IsFrontFacing || !second.IsFrontFacing)
        {
            return false;
        }

        if (edge.NormalAngleDegrees >= MathF.Max(18f, context.Settings.CreaseAngleDegrees * 0.55f))
        {
            return false;
        }

        if (curvatureFactor < 0.08f)
        {
            return false;
        }

        var viewForward = context.View.Projection.Forward;
        var start = context.Graph.Vertices[edge.StartVertexIndex];
        var end = context.Graph.Vertices[edge.EndVertexIndex];
        var startDot = MathF.Abs(Vector3.Dot(NormalizeOrDefault(start.WorldNormal), -viewForward));
        var endDot = MathF.Abs(Vector3.Dot(NormalizeOrDefault(end.WorldNormal), -viewForward));
        var nearContour = (startDot + endDot) * 0.5f;
        if (nearContour > ApparentRidgeViewDotMax)
        {
            return false;
        }

        var dotSpread = MathF.Abs(startDot - endDot);
        if (dotSpread < 0.06f)
        {
            return false;
        }

        var apparentSignal = EstimateApparentSignal(context.Graph, edge, -viewForward);
        if (apparentSignal < 0.10f)
        {
            return false;
        }

        kind = FeatureCurveKind.ApparentRidge;
        confidence = Math.Clamp(
            Math.Clamp((ApparentRidgeViewDotMax - nearContour) / ApparentRidgeViewDotMax, 0f, 1f) * 0.3f +
            Math.Clamp(dotSpread / 0.22f, 0f, 1f) * 0.25f +
            Math.Clamp(curvatureFactor / 0.25f, 0f, 1f) * 0.15f +
            Math.Clamp(apparentSignal / 0.30f, 0f, 1f) * 0.30f,
            0f,
            1f);
        return true;
    }

    private static bool TryClassifyCurvatureFeature(
        NprGraph graph,
        TopologyEdge edge,
        ProjectedTriangle first,
        ProjectedTriangle second,
        float curvatureFactor,
        out FeatureCurveKind kind,
        out float confidence)
    {
        kind = FeatureCurveKind.Accent;
        confidence = 0f;

        if (!first.IsFrontFacing || !second.IsFrontFacing)
        {
            return false;
        }

        if (edge.NormalAngleDegrees < RidgeValleyNormalAngleFloorDegrees ||
            edge.NormalAngleDegrees >= 34f)
        {
            return false;
        }

        if (curvatureFactor < 0.08f)
        {
            return false;
        }

        var shade = (first.Shade + second.Shade) * 0.5f;
        if (shade >= RidgeShadeThreshold)
        {
            kind = FeatureCurveKind.Ridge;
            confidence = Math.Clamp(
                curvatureFactor * 0.55f +
                Math.Clamp((shade - RidgeShadeThreshold) / 0.42f, 0f, 1f) * 0.2f +
                Math.Clamp((edge.NormalAngleDegrees - RidgeValleyNormalAngleFloorDegrees) / 28f, 0f, 1f) * 0.25f,
                0f,
                1f);
            return true;
        }

        if (shade <= ValleyShadeThreshold)
        {
            kind = FeatureCurveKind.Valley;
            confidence = Math.Clamp(
                curvatureFactor * 0.55f +
                Math.Clamp((ValleyShadeThreshold - shade) / 0.42f, 0f, 1f) * 0.2f +
                Math.Clamp((edge.NormalAngleDegrees - RidgeValleyNormalAngleFloorDegrees) / 28f, 0f, 1f) * 0.25f,
                0f,
                1f);
            return true;
        }

        return false;
    }

    private static float CalculateShade(NprGraph graph, TopologyEdge edge)
    {
        var firstShade = graph.Triangles[edge.FirstTriangleIndex].Shade;

        if (edge.SecondTriangleIndex < 0)
        {
            return firstShade;
        }

        return (firstShade + graph.Triangles[edge.SecondTriangleIndex].Shade) * 0.5f;
    }

    private static bool TryGetVisibleVertices(
        NprGraph graph,
        TopologyEdge edge,
        out ProjectedVertex start,
        out ProjectedVertex end)
    {
        start = graph.Vertices[edge.StartVertexIndex];
        end = graph.Vertices[edge.EndVertexIndex];

        return start.IsVisible && end.IsVisible;
    }

    private static float EstimateCurvature(NprGraph graph, TopologyEdge edge)
    {
        var start = graph.Vertices[edge.StartVertexIndex];
        var end = graph.Vertices[edge.EndVertexIndex];
        var startNormal = NormalizeOrDefault(start.WorldNormal);
        var endNormal = NormalizeOrDefault(end.WorldNormal);
        var normalDot = Math.Clamp(Vector3.Dot(startNormal, endNormal), -1f, 1f);
        var normalVariation = MathF.Acos(normalDot) / MathF.PI;

        var angular = Math.Clamp(edge.NormalAngleDegrees / 90f, 0f, 1f);
        var shadingSpread = edge.SecondTriangleIndex >= 0 &&
            (uint)edge.FirstTriangleIndex < (uint)graph.Triangles.Count &&
            (uint)edge.SecondTriangleIndex < (uint)graph.Triangles.Count
            ? MathF.Abs(graph.Triangles[edge.FirstTriangleIndex].Shade - graph.Triangles[edge.SecondTriangleIndex].Shade) * 0.15f
            : 0f;

        return Math.Clamp(normalVariation * 0.6f + angular * 0.4f + shadingSpread, 0f, 1f);
    }

    private static float EstimateRadialSignal(NprGraph graph, TopologyEdge edge, Vector3 viewDirection)
    {
        var start = graph.Vertices[edge.StartVertexIndex];
        var end = graph.Vertices[edge.EndVertexIndex];
        var avgSigned = MathF.Abs((start.SmoothedSignedCurvature + end.SmoothedSignedCurvature) * 0.5f);
        var direction = start.CurvatureDirection + end.CurvatureDirection;
        if (direction.LengthSquared() <= 0.0001f)
        {
            return avgSigned;
        }

        direction = Vector3.Normalize(direction);
        var startTangentView = ProjectOntoTangent(-viewDirection, start.WorldNormal);
        var endTangentView = ProjectOntoTangent(-viewDirection, end.WorldNormal);
        var alignment = 0.5f * (
            MathF.Abs(Vector3.Dot(direction, NormalizeOrDefault(startTangentView))) +
            MathF.Abs(Vector3.Dot(direction, NormalizeOrDefault(endTangentView))));

        return Math.Clamp(avgSigned * (0.55f + alignment * 0.45f), 0f, 1f);
    }

    private static float EstimateApparentSignal(NprGraph graph, TopologyEdge edge, Vector3 viewDirection)
    {
        var start = graph.Vertices[edge.StartVertexIndex];
        var end = graph.Vertices[edge.EndVertexIndex];
        var avgSigned = MathF.Max(0f, (start.SmoothedSignedCurvature + end.SmoothedSignedCurvature) * 0.5f);
        var radial = EstimateRadialSignal(graph, edge, viewDirection);
        var contourness = 1f - MathF.Abs(
            MathF.Abs(Vector3.Dot(NormalizeOrDefault(start.WorldNormal), viewDirection)) +
            MathF.Abs(Vector3.Dot(NormalizeOrDefault(end.WorldNormal), viewDirection))) * 0.5f;

        return Math.Clamp(avgSigned * 0.55f + radial * 0.30f + contourness * 0.15f, 0f, 1f);
    }

    private static Vector3 ProjectOntoTangent(Vector3 vector, Vector3 normal)
    {
        var projected = vector - normal * Vector3.Dot(vector, normal);
        return projected.LengthSquared() <= 0.0001f
            ? Vector3.Zero
            : Vector3.Normalize(projected);
    }

    private static Vector3 NormalizeOrDefault(Vector3 value)
    {
        return value.LengthSquared() <= 0.0001f
            ? Vector3.UnitY
            : Vector3.Normalize(value);
    }

    private static MaterialRegion? FindRegion(IReadOnlyList<MaterialRegion> regions, int projectedTriangleIndex)
    {
        for (var index = 0; index < regions.Count; index++)
        {
            var region = regions[index];
            if (region.TriangleIndices.Contains(projectedTriangleIndex))
            {
                return region;
            }
        }

        return null;
    }
}
