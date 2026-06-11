using STFU.Assets;
using STFU.Common.Primitives;
using STFU.Engine.Entities;
using STFU.Mesh;
using STFU.NPR.Pipeline;

namespace STFU.NPR.Pipeline.InteractivePerformance.Stages;

internal static class InteractiveProjectionInputBuilder
{
    public static InteractiveProjectionInput Build(NprContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var meshes = new List<InteractiveProjectionInputMesh>(context.Scene.Entities.Count);
        var entityIndex = 0;

        foreach (var entity in context.Scene.Entities)
        {
            if (!TryResolveMeshHandle(entity, out var meshHandle))
            {
                entityIndex++;
                continue;
            }

            if (!TryResolveMesh(context.Assets, meshHandle, out var mesh) || (mesh.Vertices.Count == 0 && mesh.Triangles.Count == 0))
            {
                entityIndex++;
                continue;
            }

            var role = context.EntityStyles.GetRole(entity.Id).ToString();
            meshes.Add(new InteractiveProjectionInputMesh(
                entity.Id,
                meshHandle,
                mesh,
                entity.Transform,
                entityIndex,
                role));
            entityIndex++;
        }

        if (meshes.Count == 0)
        {
            return InteractiveProjectionInput.Empty(
                context.Camera,
                context.Settings,
                context.Width,
                context.Height,
                context.FrameId,
                context.TimeSeconds,
                "Scene/assets projection input had no mesh geometry.");
        }

        return new InteractiveProjectionInput(
            meshes,
            context.Camera,
            context.Settings,
            context.Width,
            context.Height,
            context.FrameId,
            context.TimeSeconds,
            "Scene/assets projection input built without ReferenceGraph.");
    }

    private static bool TryResolveMeshHandle(Entity entity, out MeshHandle meshHandle)
    {
        meshHandle = entity.Mesh;
        return !meshHandle.IsNone;
    }

    private static bool TryResolveMesh(
        AssetRegistry assets,
        MeshHandle meshHandle,
        out MeshData mesh)
    {
        return assets.TryGetMesh(meshHandle, out mesh);
    }
}
