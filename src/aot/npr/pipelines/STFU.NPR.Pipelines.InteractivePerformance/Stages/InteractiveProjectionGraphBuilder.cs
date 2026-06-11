using System.Numerics;
using STFU.Common.Math;
using STFU.NPR.Graph;
using STFU.NPR.Pipeline;
using STFU.NPR.Projection;
using STFU.Strokes;

namespace STFU.NPR.Pipeline.InteractivePerformance.Stages;

internal static class InteractiveProjectionGraphBuilder
{
    public static NprGraph Build(InteractiveProjectionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var graph = new NprGraph();
        if (!input.HasGeometry)
        {
            return graph;
        }

        var projection = ProjectionInfo.Create(input.Camera, input.Width, input.Height, input.Settings);
        var projectedVertices = new ProjectedVertex[input.VertexCount];
        var projectedTriangles = new List<ProjectedTriangle>(input.TriangleCount);

        graph.Meshes.EnsureCapacity(input.MeshCount);
        graph.Vertices.EnsureCapacity(input.VertexCount);
        graph.Triangles.EnsureCapacity(input.TriangleCount);

        var vertexOffset = 0;
        var triangleOffset = 0;
        for (var meshIndex = 0; meshIndex < input.Meshes.Count; meshIndex++)
        {
            var meshInput = input.Meshes[meshIndex];
            if (!meshInput.HasGeometry)
            {
                continue;
            }

            var projectedMeshIndex = graph.Meshes.Count;
            MeshProjectionService.ProjectInto(
                meshInput.Mesh,
                meshInput.Transform,
                projection,
                vertexOffset,
                projectedVertices,
                vertexOffset);

            graph.Meshes.Add(new ProjectedMesh(
                meshInput.EntityId,
                meshInput.MeshHandle,
                meshInput.Mesh,
                vertexOffset,
                meshInput.VertexCount,
                triangleOffset,
                meshInput.TriangleCount));

            AppendTriangles(
                meshInput,
                projectedMeshIndex,
                vertexOffset,
                triangleOffset,
                projection.Position,
                projectedVertices,
                input.Settings.MinimumProjectedTriangleArea,
                projectedTriangles);

            vertexOffset += meshInput.VertexCount;
            triangleOffset += meshInput.TriangleCount;
        }

        graph.Vertices.AddRange(projectedVertices);
        graph.Triangles.AddRange(projectedTriangles);
        return graph;
    }

    private static void AppendTriangles(
        InteractiveProjectionInputMesh meshInput,
        int projectedMeshIndex,
        int vertexOffset,
        int triangleOffset,
        Vector3 cameraPosition,
        IReadOnlyList<ProjectedVertex> vertices,
        float minimumProjectedTriangleArea,
        List<ProjectedTriangle> triangles)
    {
        var triangleCount = meshInput.Mesh.Triangles.Count;
        for (var triangleIndex = 0; triangleIndex < triangleCount; triangleIndex++)
        {
            var triangle = meshInput.Mesh.Triangles[triangleIndex];
            var aIndex = vertexOffset + triangle.A;
            var bIndex = vertexOffset + triangle.B;
            var cIndex = vertexOffset + triangle.C;

            if ((uint)aIndex >= (uint)vertices.Count ||
                (uint)bIndex >= (uint)vertices.Count ||
                (uint)cIndex >= (uint)vertices.Count)
            {
                continue;
            }

            var a = vertices[aIndex];
            var b = vertices[bIndex];
            var c = vertices[cIndex];

            var normal = Geometry3D.TriangleNormal(a.WorldPosition, b.WorldPosition, c.WorldPosition, Vector3.UnitZ, 1e-12f);
            var worldCenter = (a.WorldPosition + b.WorldPosition + c.WorldPosition) / 3f;
            var screenCenter = new Point2D(
                (a.Position.X + b.Position.X + c.Position.X) / 3f,
                (a.Position.Y + b.Position.Y + c.Position.Y) / 3f);
            var depth = (a.Depth + b.Depth + c.Depth) / 3f;
            var screenArea = Geometry2D.SignedTriangleArea(
                a.Position.X,
                a.Position.Y,
                b.Position.X,
                b.Position.Y,
                c.Position.X,
                c.Position.Y);
            var absScreenArea = NumericMath.Abs(screenArea);
            var frontFacing = Geometry3D.IsFrontFacing(
                normal,
                worldCenter,
                cameraPosition,
                epsilonSquared: 1e-6f,
                degenerateResult: false,
                normalizeViewDirection: false);
            var visible = absScreenArea >= minimumProjectedTriangleArea &&
                (a.IsVisible || b.IsVisible || c.IsVisible);

            if (!visible)
            {
                continue;
            }

            triangles.Add(new ProjectedTriangle(
                StableId: triangleOffset + triangleIndex,
                ProjectedMeshIndex: projectedMeshIndex,
                MeshTriangleIndex: triangleIndex,
                A: aIndex,
                B: bIndex,
                C: cIndex,
                Normal: normal,
                WorldCenter: worldCenter,
                ScreenCenter: screenCenter,
                Depth: depth,
                ScreenArea: absScreenArea,
                Shade: 0f,
                IsFrontFacing: frontFacing,
                IsVisible: visible)
            {
                EntityId = meshInput.EntityId
            });
        }
    }
}
