using System.Numerics;
using STFU.NPR.Graph;
using STFU.NPR.Pipeline;
using STFU.Strokes;

namespace STFU.NPR.Steps.Mesh;

public sealed class BuildProjectedTrianglesStep : INprStep
{
    private static readonly Vector3 LightDirection = Vector3.Normalize(new Vector3(-0.35f, 0.7f, -0.45f));

    public void Execute(NprContext context)
    {
        for (var meshIndex = 0; meshIndex < context.Graph.Meshes.Count; meshIndex++)
        {
            var projectedMesh = context.Graph.Meshes[meshIndex];

            for (var triangleIndex = 0; triangleIndex < projectedMesh.Mesh.Triangles.Count; triangleIndex++)
            {
                var triangle = projectedMesh.Mesh.Triangles[triangleIndex];
                var aIndex = projectedMesh.VertexOffset + triangle.A;
                var bIndex = projectedMesh.VertexOffset + triangle.B;
                var cIndex = projectedMesh.VertexOffset + triangle.C;

                if (!IsValidVertex(projectedMesh, triangle.A) ||
                    !IsValidVertex(projectedMesh, triangle.B) ||
                    !IsValidVertex(projectedMesh, triangle.C))
                {
                    continue;
                }

                var a = context.Graph.Vertices[aIndex];
                var b = context.Graph.Vertices[bIndex];
                var c = context.Graph.Vertices[cIndex];
                var normal = CalculateNormal(a.WorldPosition, b.WorldPosition, c.WorldPosition);
                var worldCenter = (a.WorldPosition + b.WorldPosition + c.WorldPosition) / 3f;
                var screenCenter = Average(a.Position, b.Position, c.Position);
                var depth = (a.Depth + b.Depth + c.Depth) / 3f;
                var shade = 1f - Math.Clamp(Vector3.Dot(normal, LightDirection) * 0.5f + 0.5f, 0f, 1f);
                var viewDirection = Vector3.Normalize(context.Camera.Position - worldCenter);
                var isFrontFacing = Vector3.Dot(normal, viewDirection) > 0f;
                var isVisible = a.IsVisible && b.IsVisible && c.IsVisible;
                var screenArea = CalculateArea(a.Position, b.Position, c.Position);

                context.Graph.Triangles.Add(new ProjectedTriangle(
                    StableTriangleId(meshIndex, triangleIndex),
                    meshIndex,
                    triangleIndex,
                    aIndex,
                    bIndex,
                    cIndex,
                    normal,
                    worldCenter,
                    screenCenter,
                    depth,
                    screenArea,
                    shade,
                    isFrontFacing,
                    isVisible));
            }
        }
    }

    private static Vector3 CalculateNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        var normal = Vector3.Cross(b - a, c - a);
        return normal.LengthSquared() <= 0.0001f ? Vector3.UnitY : Vector3.Normalize(normal);
    }

    private static Point2D Average(Point2D a, Point2D b, Point2D c)
    {
        return new Point2D((a.X + b.X + c.X) / 3f, (a.Y + b.Y + c.Y) / 3f);
    }

    private static float CalculateArea(Point2D a, Point2D b, Point2D c)
    {
        return MathF.Abs((a.X * (b.Y - c.Y) + b.X * (c.Y - a.Y) + c.X * (a.Y - b.Y)) * 0.5f);
    }

    private static bool IsValidVertex(ProjectedMesh mesh, int vertexIndex)
    {
        return vertexIndex >= 0 && vertexIndex < mesh.VertexCount;
    }

    private static int StableTriangleId(int meshIndex, int triangleIndex)
    {
        unchecked
        {
            return meshIndex * 73856093 ^ triangleIndex * 19349663;
        }
    }
}
