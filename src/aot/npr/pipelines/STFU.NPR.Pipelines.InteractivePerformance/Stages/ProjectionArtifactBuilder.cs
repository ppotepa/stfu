using STFU.NPR.Graph;
using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Core;

namespace STFU.NPR.Pipeline.InteractivePerformance.Stages;

public static class ProjectionArtifactBuilder
{
    public static ProjectedVertexArtifact BuildVertices(
        InteractiveFrameContext context,
        ArtifactKey key)
    {
        ArgumentNullException.ThrowIfNull(context);

        var graphVertices = context.ReferenceContext.Graph.Vertices;
        if (graphVertices.Count == 0)
        {
            return new ProjectedVertexArtifact
            {
                Key = key,
                Revision = context.Intent.FrameId,
                LastBuildTime = TimeSpan.Zero,
                VisibleVertexCount = 0,
                Vertices = [],
                Note = "Reference graph did not contain projected vertices."
            };
        }

        var vertices = new InteractiveProjectedVertex[graphVertices.Count];
        var visibleCount = 0;
        for (var index = 0; index < graphVertices.Count; index++)
        {
            var vertex = graphVertices[index];
            if (vertex.IsVisible)
            {
                visibleCount++;
            }

            vertices[index] = ToInteractiveVertex(index, vertex);
        }

        return new ProjectedVertexArtifact
        {
            Key = key,
            Revision = context.Intent.FrameId,
            LastBuildTime = TimeSpan.Zero,
            VisibleVertexCount = visibleCount,
            Vertices = vertices,
            Note = "Projected vertices harvested from the Reference Quality graph."
        };
    }

    public static ProjectedTriangleArtifact BuildTriangles(
        InteractiveFrameContext context,
        ArtifactKey key)
    {
        ArgumentNullException.ThrowIfNull(context);

        var graphTriangles = context.ReferenceContext.Graph.Triangles;
        if (graphTriangles.Count == 0)
        {
            return new ProjectedTriangleArtifact
            {
                Key = key,
                Revision = context.Intent.FrameId,
                LastBuildTime = TimeSpan.Zero,
                FrontFacingTriangleCount = 0,
                VisibleTriangleCount = 0,
                Triangles = [],
                Note = "Reference graph did not contain projected triangles."
            };
        }

        var triangles = new InteractiveProjectedTriangle[graphTriangles.Count];
        var frontFacingCount = 0;
        var visibleCount = 0;
        for (var index = 0; index < graphTriangles.Count; index++)
        {
            var triangle = graphTriangles[index];
            if (triangle.IsFrontFacing)
            {
                frontFacingCount++;
            }

            if (triangle.IsVisible)
            {
                visibleCount++;
            }

            triangles[index] = ToInteractiveTriangle(index, triangle);
        }

        return new ProjectedTriangleArtifact
        {
            Key = key,
            Revision = context.Intent.FrameId,
            LastBuildTime = TimeSpan.Zero,
            FrontFacingTriangleCount = frontFacingCount,
            VisibleTriangleCount = visibleCount,
            Triangles = triangles,
            Note = "Projected triangles harvested from the Reference Quality graph."
        };
    }

    private static InteractiveProjectedVertex ToInteractiveVertex(
        int sourceIndex,
        ProjectedVertex vertex)
    {
        return new InteractiveProjectedVertex(
            SourceIndex: sourceIndex,
            MeshVertexIndex: vertex.MeshVertexIndex,
            X: vertex.Position.X,
            Y: vertex.Position.Y,
            Depth: vertex.Depth,
            Depth01: vertex.Depth01,
            IsVisible: vertex.IsVisible,
            WorldX: vertex.WorldPosition.X,
            WorldY: vertex.WorldPosition.Y,
            WorldZ: vertex.WorldPosition.Z,
            NormalX: vertex.WorldNormal.X,
            NormalY: vertex.WorldNormal.Y,
            NormalZ: vertex.WorldNormal.Z,
            NdcX: vertex.Ndc.X,
            NdcY: vertex.Ndc.Y,
            NdcZ: vertex.Ndc.Z);
    }

    private static InteractiveProjectedTriangle ToInteractiveTriangle(
        int sourceIndex,
        ProjectedTriangle triangle)
    {
        return new InteractiveProjectedTriangle(
            SourceIndex: sourceIndex,
            StableId: triangle.StableId,
            ProjectedMeshIndex: triangle.ProjectedMeshIndex,
            MeshTriangleIndex: triangle.MeshTriangleIndex,
            A: triangle.A,
            B: triangle.B,
            C: triangle.C,
            ScreenCenterX: triangle.ScreenCenter.X,
            ScreenCenterY: triangle.ScreenCenter.Y,
            WorldCenterX: triangle.WorldCenter.X,
            WorldCenterY: triangle.WorldCenter.Y,
            WorldCenterZ: triangle.WorldCenter.Z,
            NormalX: triangle.Normal.X,
            NormalY: triangle.Normal.Y,
            NormalZ: triangle.Normal.Z,
            Depth: triangle.Depth,
            ScreenArea: triangle.ScreenArea,
            Shade: triangle.Shade,
            IsFrontFacing: triangle.IsFrontFacing,
            IsVisible: triangle.IsVisible);
    }
}
