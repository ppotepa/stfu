using System.Numerics;
using STFU.Common.Math;
using STFU.Mesh;
using STFU.NPR.Graph;

namespace STFU.NPR.Pipeline.Default.Steps;

public sealed class ProjectMeshStep : STFU.NPR.Pipeline.INprStep
{
    public void Execute(STFU.NPR.Pipeline.NprContext context)
    {
        var projection = context.View.Projection;

        foreach (var entity in context.Scene.Entities)
        {
            if (!context.Assets.TryGetMesh(entity.Mesh, out var mesh) || mesh.Vertices.Count == 0)
            {
                continue;
            }

            var vertexOffset = context.Graph.Vertices.Count;
            var triangleOffset = context.Graph.Triangles.Count;
            context.Graph.Vertices.EnsureCapacity(vertexOffset + mesh.Vertices.Count);
            context.Graph.Meshes.EnsureCapacity(context.Graph.Meshes.Count + 1);
            var transform = entity.Transform;
            var hasRotation = HasRotation(transform.Rotation);
            var rotation = hasRotation ? CreateRotation(transform.Rotation) : Quaternion.Identity;
            var normalMatrix = hasRotation ? Matrix4x4.CreateFromQuaternion(rotation) : Matrix4x4.Identity;

            for (var vertexIndex = 0; vertexIndex < mesh.Vertices.Count; vertexIndex++)
            {
                var vertex = mesh.Vertices[vertexIndex];
                var worldPosition = hasRotation
                    ? TransformPosition(vertex.Position, transform, rotation)
                    : vertex.Position * transform.Scale + transform.Position;
                var worldNormal = TransformNormal(vertex.Normal, normalMatrix, hasRotation);
                var isVisible = projection.TryProject(worldPosition, out var position, out var depth, out var ndc, out var depth01);

                context.Graph.Vertices.Add(new ProjectedVertex(
                    vertexOffset + vertexIndex,
                    worldPosition,
                    worldNormal,
                    position,
                    depth,
                    isVisible,
                    0f,
                    0f,
                    0f,
                    0f,
                    Vector3.Zero,
                    ndc,
                    depth01));
            }

            context.Graph.Meshes.Add(new ProjectedMesh(
                entity.Id,
                entity.Mesh,
                mesh,
                vertexOffset,
                mesh.Vertices.Count,
                triangleOffset,
                mesh.Triangles.Count));
        }
    }

    private static Vector3 TransformPosition(Vector3 position, Transform3D transform, Quaternion rotation)
    {
        return Vector3.Transform(position * transform.Scale, rotation) + transform.Position;
    }

    private static Vector3 TransformNormal(Vector3 normal, Matrix4x4 normalMatrix, bool hasRotation)
    {
        if (normal.LengthSquared() <= 1e-6f)
        {
            return Vector3.UnitZ;
        }

        return hasRotation
            ? Vector3.Normalize(Vector3.TransformNormal(normal, normalMatrix))
            : Vector3.Normalize(normal);
    }

    private static Quaternion CreateRotation(Vector3 rotation)
    {
        return Quaternion.CreateFromYawPitchRoll(rotation.Y, rotation.X, rotation.Z);
    }

    private static bool HasRotation(Vector3 rotation)
    {
        return rotation.LengthSquared() > 0.0000001f;
    }
}
