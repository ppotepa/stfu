using STFU.NPR.Graph;
using STFU.NPR.Pipeline;

namespace STFU.NPR.Steps.Mesh;

public sealed class ExtractFeatureLinesStep : INprStep
{
    public void Execute(NprContext context)
    {
        foreach (var edge in context.Graph.TopologyEdges)
        {
            if (!TryGetVisibleVertices(context.Graph, edge, out var start, out var end))
            {
                continue;
            }

            var intent = Classify(context.Graph, edge, context.Settings.CreaseAngleDegrees);
            if (intent is null)
            {
                continue;
            }

            var shade = CalculateShade(context.Graph, edge);
            var angleFactor = Math.Clamp(edge.NormalAngleDegrees / 90f, 0f, 1f);
            var depth = (start.Depth + end.Depth) * 0.5f;
            var importance = intent.Value switch
            {
                NprStrokeIntent.Silhouette => 1.0f,
                NprStrokeIntent.Boundary => 0.9f,
                NprStrokeIntent.Crease => 0.55f + angleFactor * 0.35f + shade * 0.08f,
                _ => 0.35f
            };

            context.Graph.FeatureLines.Add(new FeatureLine(
                edge.StableId,
                intent.Value,
                start.Position,
                end.Position,
                depth,
                shade,
                Math.Clamp(importance, 0f, 1f)));
        }
    }

    private static NprStrokeIntent? Classify(NprGraph graph, TopologyEdge edge, float creaseAngleDegrees)
    {
        if (edge.IsBoundary)
        {
            return NprStrokeIntent.Boundary;
        }

        var first = graph.Triangles[edge.FirstTriangleIndex];
        var second = graph.Triangles[edge.SecondTriangleIndex];

        if (first.IsFrontFacing != second.IsFrontFacing)
        {
            return NprStrokeIntent.Silhouette;
        }

        if (edge.NormalAngleDegrees >= creaseAngleDegrees && first.IsFrontFacing && second.IsFrontFacing)
        {
            return NprStrokeIntent.Crease;
        }

        return null;
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
}
