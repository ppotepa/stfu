using System.Runtime.InteropServices;

namespace STFU.Import.Fbx;

public static class FbxNativeAbiSmoke
{
    public static FbxNativeAbiSmokeResult Verify(string sourcePath)
    {
        var vertexSize = Marshal.SizeOf<FbxNativeVertex>();
        if (vertexSize != 28)
        {
            throw new InvalidOperationException(
                $"Managed FbxNativeVertex size is {vertexSize} bytes, expected 28 bytes.");
        }

        using var sampler = FbxBakedAnimationSampler.Load(sourcePath);
        var mesh = sampler.BakeCombinedMesh(animationIndex: -1, timeSeconds: 0);
        if (mesh.Vertices.Count == 0 || mesh.Triangles.Count == 0)
        {
            throw new InvalidOperationException("FBX ABI smoke produced an empty mesh.");
        }

        if (mesh.LogicalVertexIds is null || mesh.LogicalVertexIds.Count != mesh.Vertices.Count)
        {
            throw new InvalidOperationException("FBX ABI smoke did not receive logical vertex ids.");
        }

        var distinctLogicalIds = new HashSet<int>();
        for (var i = 0; i < mesh.LogicalVertexIds.Count; i++)
        {
            distinctLogicalIds.Add(mesh.LogicalVertexIds[i]);
        }

        return new FbxNativeAbiSmokeResult(
            VertexStructBytes: vertexSize,
            VertexCount: mesh.Vertices.Count,
            TriangleCount: mesh.Triangles.Count,
            LogicalVertexIdCount: mesh.LogicalVertexIds.Count,
            DistinctLogicalVertexIdCount: distinctLogicalIds.Count);
    }
}

public sealed record FbxNativeAbiSmokeResult(
    int VertexStructBytes,
    int VertexCount,
    int TriangleCount,
    int LogicalVertexIdCount,
    int DistinctLogicalVertexIdCount);
