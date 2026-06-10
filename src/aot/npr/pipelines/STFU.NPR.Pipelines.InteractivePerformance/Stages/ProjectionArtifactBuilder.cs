using STFU.NPR.Graph;
using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.NPR.Pipelines.Abstractions;

namespace STFU.NPR.Pipeline.InteractivePerformance.Stages;

public static class ProjectionArtifactBuilder
{
    public static InteractiveProjectionArtifactSet BuildAll(
        InteractiveFrameContext context,
        ArtifactKey summaryKey,
        ArtifactKey verticesKey,
        ArtifactKey trianglesKey,
        FramePipelineStrategyOptions options)
    {
        ArgumentNullException.ThrowIfNull(context);
        options ??= FramePipelineStrategyOptions.Default;

        var snapshot = ResolveProjectionSnapshot(context, options);
        var vertices = BuildVerticesFromSnapshot(context, verticesKey, snapshot);
        var triangles = BuildTrianglesFromSnapshot(context, trianglesKey, snapshot);
        var summary = BuildSummary(context, summaryKey, snapshot, vertices, triangles);
        return new InteractiveProjectionArtifactSet(summary, vertices, triangles);
    }

    public static ProjectedVertexArtifact BuildVertices(
        InteractiveFrameContext context,
        ArtifactKey key)
    {
        return BuildAll(
            context,
            ArtifactKeyFactory.ProjectionSummary(context.Intent),
            key,
            ArtifactKeyFactory.ProjectedTriangles(context.Intent, context.ReferenceContext.Graph.Triangles.Count),
            FramePipelineStrategyOptions.Default).Vertices;
    }

    public static ProjectedTriangleArtifact BuildTriangles(
        InteractiveFrameContext context,
        ArtifactKey key)
    {
        return BuildAll(
            context,
            ArtifactKeyFactory.ProjectionSummary(context.Intent),
            ArtifactKeyFactory.ProjectedVertices(context.Intent, context.ReferenceContext.Graph.Vertices.Count),
            key,
            FramePipelineStrategyOptions.Default).Triangles;
    }

    private static InteractiveProjectionSnapshot ResolveProjectionSnapshot(
        InteractiveFrameContext context,
        FramePipelineStrategyOptions options)
    {
        if (options.EnableSelfContainedProjection && options.PreferSelfContainedProjection)
        {
            var scratch = InteractiveProjectionScratchBuilder.Build(context);
            if (scratch.ProjectedVertexCount > 0 || scratch.ProjectedTriangleCount > 0)
            {
                return scratch;
            }
        }

        var reference = BuildReferenceGraphSnapshot(context);
        if (reference.ProjectedVertexCount > 0 || reference.ProjectedTriangleCount > 0 || !options.EnableSelfContainedProjection)
        {
            return reference;
        }

        return InteractiveProjectionScratchBuilder.Build(context);
    }

    private static InteractiveProjectionSnapshot BuildReferenceGraphSnapshot(InteractiveFrameContext context)
    {
        var graph = context.ReferenceContext.Graph;
        return new InteractiveProjectionSnapshot(
            graph,
            InteractiveProjectionSource.ReferenceGraph,
            SourceEntityCount: context.ReferenceContext.Scene.Entities.Count,
            ProjectedMeshCount: graph.Meshes.Count,
            ProjectedVertexCount: graph.Vertices.Count,
            ProjectedTriangleCount: graph.Triangles.Count,
            Note: graph.Vertices.Count > 0 || graph.Triangles.Count > 0
                ? "Projected geometry harvested from the populated Reference Quality graph."
                : "Reference Quality graph did not contain projected geometry.");
    }

    private static ProjectedVertexArtifact BuildVerticesFromSnapshot(
        InteractiveFrameContext context,
        ArtifactKey key,
        InteractiveProjectionSnapshot snapshot)
    {
        var graphVertices = snapshot.Graph.Vertices;
        if (graphVertices.Count == 0)
        {
            return new ProjectedVertexArtifact
            {
                Key = key,
                Revision = context.Intent.FrameId,
                LastBuildTime = TimeSpan.Zero,
                VisibleVertexCount = 0,
                Vertices = [],
                Source = snapshot.Source,
                Note = snapshot.Note
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
            Source = snapshot.Source,
            Note = snapshot.Note
        };
    }

    private static ProjectedTriangleArtifact BuildTrianglesFromSnapshot(
        InteractiveFrameContext context,
        ArtifactKey key,
        InteractiveProjectionSnapshot snapshot)
    {
        var graphTriangles = snapshot.Graph.Triangles;
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
                Source = snapshot.Source,
                Note = snapshot.Note
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
            Source = snapshot.Source,
            Note = snapshot.Note
        };
    }

    private static ProjectionSummaryArtifact BuildSummary(
        InteractiveFrameContext context,
        ArtifactKey key,
        InteractiveProjectionSnapshot snapshot,
        ProjectedVertexArtifact vertices,
        ProjectedTriangleArtifact triangles)
    {
        var fullProjectionAvailable = vertices.VertexCount > 0 || triangles.TriangleCount > 0;
        return new ProjectionSummaryArtifact
        {
            Key = key,
            Revision = context.Intent.FrameId,
            Width = context.Intent.Width,
            Height = context.Intent.Height,
            FullProjectionAvailable = fullProjectionAvailable,
            LastBuildTime = TimeSpan.Zero,
            Source = snapshot.Source,
            SourceEntityCount = snapshot.SourceEntityCount,
            ProjectedMeshCount = snapshot.ProjectedMeshCount,
            ProjectedVertexCount = vertices.VertexCount,
            ProjectedTriangleCount = triangles.TriangleCount,
            Note = fullProjectionAvailable
                ? snapshot.Note
                : "Interactive projection artifacts are empty because no projectable geometry was found."
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
