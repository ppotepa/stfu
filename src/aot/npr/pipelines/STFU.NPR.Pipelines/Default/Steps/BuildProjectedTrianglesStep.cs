using System.Numerics;
using STFU.NPR.Graph;
using STFU.Strokes;

namespace STFU.NPR.Pipeline.Default.Steps;

public sealed class BuildProjectedTrianglesStep : STFU.NPR.Pipeline.INprStep
{
    public void Execute(STFU.NPR.Pipeline.NprContext context)
    {
        var triangleCapacity = context.Graph.Triangles.Count;
        for (var meshIndex = 0; meshIndex < context.Graph.Meshes.Count; meshIndex++)
        {
            triangleCapacity += context.Graph.Meshes[meshIndex].Mesh.Triangles.Count;
        }

        context.Graph.Triangles.EnsureCapacity(triangleCapacity);

        for (var meshIndex = 0; meshIndex < context.Graph.Meshes.Count; meshIndex++)
        {
            var projectedMesh = context.Graph.Meshes[meshIndex];
            for (var triangleIndex = 0; triangleIndex < projectedMesh.Mesh.Triangles.Count; triangleIndex++)
            {
                var triangle = projectedMesh.Mesh.Triangles[triangleIndex];
                var aIndex = projectedMesh.VertexOffset + triangle.A;
                var bIndex = projectedMesh.VertexOffset + triangle.B;
                var cIndex = projectedMesh.VertexOffset + triangle.C;

                if ((uint)aIndex >= (uint)context.Graph.Vertices.Count ||
                    (uint)bIndex >= (uint)context.Graph.Vertices.Count ||
                    (uint)cIndex >= (uint)context.Graph.Vertices.Count)
                {
                    continue;
                }

                var a = context.Graph.Vertices[aIndex];
                var b = context.Graph.Vertices[bIndex];
                var c = context.Graph.Vertices[cIndex];

                var normal = Vector3.Cross(b.WorldPosition - a.WorldPosition, c.WorldPosition - a.WorldPosition);
                normal = normal.LengthSquared() <= 1e-12f
                    ? Vector3.UnitZ
                    : Vector3.Normalize(normal);
                var worldCenter = (a.WorldPosition + b.WorldPosition + c.WorldPosition) / 3f;
                var screenCenter = new Point2D(
                    (a.Position.X + b.Position.X + c.Position.X) / 3f,
                    (a.Position.Y + b.Position.Y + c.Position.Y) / 3f);
                var depth = (a.Depth + b.Depth + c.Depth) / 3f;
                var screenArea = SignedArea(a.Position, b.Position, c.Position);
                var frontFacing = IsFrontFacing(normal, worldCenter, context.Camera.Position);
                var visible = MathF.Abs(screenArea) >= context.Settings.MinimumProjectedTriangleArea &&
                    (a.IsVisible || b.IsVisible || c.IsVisible);

                context.Graph.Triangles.Add(new ProjectedTriangle(
                    StableId: projectedMesh.TriangleOffset + triangleIndex,
                    ProjectedMeshIndex: meshIndex,
                    MeshTriangleIndex: triangleIndex,
                    A: aIndex,
                    B: bIndex,
                    C: cIndex,
                    Normal: normal,
                    WorldCenter: worldCenter,
                    ScreenCenter: screenCenter,
                    Depth: depth,
                    ScreenArea: MathF.Abs(screenArea),
                    Shade: 0f,
                    IsFrontFacing: frontFacing,
                    IsVisible: visible)
                {
                    EntityId = projectedMesh.EntityId
                });
            }
        }
    }

    private static float SignedArea(Point2D a, Point2D b, Point2D c)
    {
        return ((b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X)) * 0.5f;
    }

    private static bool IsFrontFacing(Vector3 normal, Vector3 center, Vector3 cameraPosition)
    {
        var viewDirection = cameraPosition - center;
        if (viewDirection.LengthSquared() <= 1e-6f)
        {
            return false;
        }

        return Vector3.Dot(normal, viewDirection) > 0f;
    }
}
