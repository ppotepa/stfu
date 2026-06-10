using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.NPR.Pipeline.InteractivePerformance.Stages;
using Xunit;

namespace STFU.NPR.Pipelines.Tests;

public sealed class InteractiveSelfContainedCandidateEdgeTests
{
    [Fact]
    public void Projected_triangle_candidate_builder_emits_edges_from_visible_triangles()
    {
        var vertices = new[]
        {
            CreateVertex(0, 0, 0),
            CreateVertex(1, 10, 0),
            CreateVertex(2, 0, 10),
            CreateVertex(3, 10, 10)
        };
        var triangles = new[]
        {
            CreateTriangle(0, 0, 1, 2, visible: true),
            CreateTriangle(1, 1, 3, 2, visible: true)
        };

        var edges = ProjectedTriangleCandidateEdgeBuilder.BuildEdges(triangles, vertices, visibleFaces: null);

        Assert.NotEmpty(edges);
        Assert.All(edges, edge => Assert.True(edge.ProjectedLength > 0));
        Assert.Contains(edges, edge => edge.FaceA == 0);
        Assert.Contains(edges, edge => edge.FaceA == 1 || edge.FaceB == 1);
    }

    [Fact]
    public void Projected_triangle_candidate_builder_filters_by_visible_face_set()
    {
        var vertices = new[]
        {
            CreateVertex(0, 0, 0),
            CreateVertex(1, 10, 0),
            CreateVertex(2, 0, 10),
            CreateVertex(3, 10, 10)
        };
        var triangles = new[]
        {
            CreateTriangle(0, 0, 1, 2, visible: true),
            CreateTriangle(1, 1, 3, 2, visible: true)
        };

        var edges = ProjectedTriangleCandidateEdgeBuilder.BuildEdges(
            triangles,
            vertices,
            new HashSet<int> { 1 });

        Assert.NotEmpty(edges);
        Assert.All(edges, edge => Assert.True(edge.FaceA == 1 || edge.FaceB == 1));
    }

    [Fact]
    public void Projected_triangle_candidate_builder_ignores_hidden_triangles_and_vertices()
    {
        var vertices = new[]
        {
            CreateVertex(0, 0, 0, visible: true),
            CreateVertex(1, 10, 0, visible: false),
            CreateVertex(2, 0, 10, visible: true)
        };
        var triangles = new[]
        {
            CreateTriangle(0, 0, 1, 2, visible: true),
            CreateTriangle(1, 0, 2, 1, visible: false)
        };

        var edges = ProjectedTriangleCandidateEdgeBuilder.BuildEdges(triangles, vertices, visibleFaces: null);

        Assert.NotEmpty(edges);
        Assert.DoesNotContain(edges, edge => edge.X0 == 10 || edge.X1 == 10);
    }

    [Fact]
    public void Candidate_edge_source_enum_reserves_projected_triangle_source_for_self_contained_path()
    {
        Assert.Equal(0, (long)InteractiveCandidateEdgeSource.None);
        Assert.Equal(1, (long)InteractiveCandidateEdgeSource.ReferenceFragments);
        Assert.Equal(2, (long)InteractiveCandidateEdgeSource.ProjectedTriangleEdges);
    }

    private static InteractiveProjectedVertex CreateVertex(
        int index,
        float x,
        float y,
        bool visible = true)
    {
        return new InteractiveProjectedVertex(
            SourceIndex: index,
            MeshVertexIndex: index,
            X: x,
            Y: y,
            Depth: 0.5f,
            Depth01: 0.5f,
            IsVisible: visible,
            WorldX: x,
            WorldY: y,
            WorldZ: 0,
            NormalX: 0,
            NormalY: 0,
            NormalZ: 1,
            NdcX: 0,
            NdcY: 0,
            NdcZ: 0);
    }

    private static InteractiveProjectedTriangle CreateTriangle(
        int sourceIndex,
        int a,
        int b,
        int c,
        bool visible)
    {
        return new InteractiveProjectedTriangle(
            SourceIndex: sourceIndex,
            StableId: 100 + sourceIndex,
            ProjectedMeshIndex: 0,
            MeshTriangleIndex: sourceIndex,
            A: a,
            B: b,
            C: c,
            ScreenCenterX: 5,
            ScreenCenterY: 5,
            WorldCenterX: 0,
            WorldCenterY: 0,
            WorldCenterZ: 0,
            NormalX: 0,
            NormalY: 0,
            NormalZ: 1,
            Depth: 0.5f,
            ScreenArea: 20,
            Shade: 0.5f,
            IsFrontFacing: true,
            IsVisible: visible);
    }
}
