namespace STFU.NPR.Graph;

public sealed class NprGraph
{
    public List<ProjectedMesh> Meshes { get; } = [];

    public List<ProjectedVertex> Vertices { get; } = [];

    public List<ProjectedTriangle> Triangles { get; } = [];

    public List<TopologyEdge> TopologyEdges { get; } = [];

    public List<ProjectedEdge> Edges { get; } = [];

    public List<SurfaceSample> SurfaceSamples { get; } = [];

    public List<FeatureLine> FeatureLines { get; } = [];

    public List<NprStroke> Strokes { get; } = [];

    public void Clear()
    {
        Meshes.Clear();
        Vertices.Clear();
        Triangles.Clear();
        TopologyEdges.Clear();
        Edges.Clear();
        SurfaceSamples.Clear();
        FeatureLines.Clear();
        Strokes.Clear();
    }
}
