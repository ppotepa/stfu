using STFU.NPR.Graph;

namespace STFU.NPR.Analysis;

public sealed class ProjectedMeshFrame
{
    public ProjectedMeshFrame(ProjectedVertex[] vertices, int vertexCount)
    {
        Vertices = vertices;
        VertexCount = vertexCount;
    }

    public ProjectedVertex[] Vertices { get; }
    public int VertexCount { get; }
}
