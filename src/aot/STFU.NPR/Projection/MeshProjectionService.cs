using System.Numerics;
using System.Runtime.InteropServices;
using STFU.Common.Math;
using STFU.Mesh;
using STFU.NPR.Analysis;
using STFU.NPR.Graph;
using STFU.NPR.Pipeline;

namespace STFU.NPR.Projection;

public static class MeshProjectionService
{
    private const float RotationEpsilonSquared = 0.0000001f;
    private const float ScaleEpsilonSquared = 0.0000001f;
    private const float TranslationEpsilonSquared = 0.0000001f;

    public static ProjectedMeshFrame Project(
        MeshData mesh,
        Transform3D transform,
        ProjectionInfo projection,
        int meshVertexOffset)
    {
        var output = new ProjectedVertex[mesh.Vertices.Count];
        ProjectInto(mesh, transform, projection, meshVertexOffset, output, 0);
        return new ProjectedMeshFrame(output, output.Length);
    }

    public static void ProjectInto(
        MeshData mesh,
        Transform3D transform,
        ProjectionInfo projection,
        int meshVertexOffset,
        ProjectedVertex[] output,
        int outputOffset)
    {
        var hasRotation = Geometry3D.HasVectorLength(transform.Rotation, RotationEpsilonSquared);
        var hasScale = Geometry3D.HasNonIdentityScale(transform.Scale, ScaleEpsilonSquared);
        var hasTranslation = Geometry3D.HasVectorLength(transform.Position, TranslationEpsilonSquared);
        var rotation = hasRotation
            ? Geometry3D.CreateYawPitchRollRotation(transform.Rotation)
            : Quaternion.Identity;

        if (TryGetVertexSpan(mesh.Vertices, out var vertices))
        {
            for (var index = 0; index < vertices.Length; index++)
            {
                output[outputOffset + index] = ProjectVertex(
                    projection,
                    vertices[index],
                    transform,
                    rotation,
                    hasRotation,
                    hasScale,
                    hasTranslation,
                    meshVertexOffset + index);
            }

            return;
        }

        for (var index = 0; index < mesh.Vertices.Count; index++)
        {
            output[outputOffset + index] = ProjectVertex(
                projection,
                mesh.Vertices[index],
                transform,
                rotation,
                hasRotation,
                hasScale,
                hasTranslation,
                meshVertexOffset + index);
        }
    }

    private static ProjectedVertex ProjectVertex(
        ProjectionInfo projection,
        MeshVertex vertex,
        Transform3D transform,
        Quaternion rotation,
        bool hasRotation,
        bool hasScale,
        bool hasTranslation,
        int meshVertexIndex)
    {
        var worldPosition = Geometry3D.TransformPosition(
            vertex.Position,
            transform.Scale,
            rotation,
            transform.Position,
            hasRotation,
            hasScale,
            hasTranslation);

        var isVisible = projection.TryProject(
            worldPosition,
            out var position,
            out var depth,
            out var ndc,
            out var depth01);

        return new ProjectedVertex(
            meshVertexIndex,
            worldPosition,
            vertex.Normal,
            position,
            depth,
            isVisible,
            0f,
            0f,
            0f,
            0f,
            Vector3.Zero,
            ndc,
            depth01);
    }

    private static bool TryGetVertexSpan(
        IReadOnlyList<MeshVertex> source,
        out ReadOnlySpan<MeshVertex> vertices)
    {
        switch (source)
        {
            case MeshVertex[] array:
                vertices = array;
                return true;
            case List<MeshVertex> list:
                vertices = CollectionsMarshal.AsSpan(list);
                return true;
            default:
                vertices = default;
                return false;
        }
    }
}
