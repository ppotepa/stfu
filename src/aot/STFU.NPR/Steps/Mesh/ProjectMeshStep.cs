using System.Numerics;
using STFU.Common.Math;
using STFU.NPR.Graph;
using STFU.NPR.Pipeline;

namespace STFU.NPR.Steps.Mesh;

public sealed class ProjectMeshStep : INprStep
{
    public void Execute(NprContext context)
    {
        var projector = CameraProjector.Create(context.Camera, context.Width, context.Height);

        foreach (var entity in context.Scene.Entities)
        {
            if (!context.Assets.TryGetMesh(entity.Mesh, out var mesh))
            {
                continue;
            }

            var vertexOffset = context.Graph.Vertices.Count;
            var triangleOffset = context.Graph.Triangles.Count;
            context.Graph.Meshes.Add(new ProjectedMesh(
                mesh,
                vertexOffset,
                mesh.Vertices.Count,
                triangleOffset,
                mesh.Triangles.Count));

            for (var index = 0; index < mesh.Vertices.Count; index++)
            {
                var worldPosition = Transform(mesh.Vertices[index].Position, entity.Transform);
                var worldNormal = TransformNormal(mesh.Vertices[index].Normal, entity.Transform);
                var isVisible = projector.TryProject(worldPosition, out var projected, out var depth);
                context.Graph.Vertices.Add(new ProjectedVertex(
                    index,
                    worldPosition,
                    worldNormal,
                    projected,
                    depth,
                    isVisible));
            }
        }
    }

    private static Vector3 Transform(Vector3 position, Transform3D transform)
    {
        return position * transform.Scale + transform.Position;
    }

    private static Vector3 TransformNormal(Vector3 normal, Transform3D transform)
    {
        var scaled = normal * transform.Scale;

        return scaled.LengthSquared() <= 0.0001f
            ? Vector3.UnitY
            : Vector3.Normalize(scaled);
    }
}
