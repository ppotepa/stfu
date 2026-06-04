using System.Numerics;
using STFU.Animation.Clips;
using STFU.Mesh;

namespace STFU.Import.Fbx;

public sealed class FbxBakedAnimationSampler : IDisposable
{
    private readonly NativeFbxSceneHandle _scene;
    private readonly FbxNativeSceneInfo _info;
    private int[]? _meshVertexOffsets;
    private int[]? _meshVertexCounts;
    private int[]? _meshTriangleCounts;
    private MeshVertex[]? _combinedVertices;
    private MeshTriangle[]? _combinedTriangles;
    private MeshData? _combinedMesh;
    private int _combinedVertexCount;
    private bool _disposed;

    private FbxBakedAnimationSampler(string sourcePath, NativeFbxSceneHandle scene, FbxNativeSceneInfo info)
    {
        SourcePath = sourcePath;
        _scene = scene;
        _info = info;
        Animations = LoadAnimations(scene.DangerousGetHandle(), info);
    }

    public string SourcePath { get; }

    public int MeshCount => _info.MeshCount;

    public IReadOnlyList<AnimationClip> Animations { get; }

    public static FbxBakedAnimationSampler Load(string sourcePath)
    {
        var rawScene = FbxNative.Load(sourcePath, out var error);
        if (rawScene == 0)
        {
            throw new InvalidOperationException(error.GetMessage());
        }

        var scene = NativeFbxSceneHandle.FromRaw(rawScene);
        var infoStatus = FbxNative.GetSceneInfo(scene.DangerousGetHandle(), out var info);
        if (infoStatus != 0)
        {
            scene.Dispose();
            throw new InvalidOperationException($"FBX native scene info failed with status {infoStatus}.");
        }

        return new FbxBakedAnimationSampler(sourcePath, scene, info);
    }

    public MeshData BakeCombinedMesh(int animationIndex, double timeSeconds)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_combinedTriangles is not null)
        {
            return BakeCombinedMeshWithCachedTopology(animationIndex, timeSeconds);
        }

        var meshes = new MeshData[Math.Max(0, _info.MeshCount)];
        for (var i = 0; i < meshes.Length; i++)
        {
            var bakeStatus = FbxNative.BakeMeshAtTime(
                _scene.DangerousGetHandle(),
                i,
                animationIndex,
                (float)timeSeconds,
                out var buffer);

            if (bakeStatus != 0)
            {
                throw new InvalidOperationException($"FBX native mesh bake failed for mesh {i} with status {bakeStatus}.");
            }

            try
            {
                meshes[i] = buffer.ToMeshData();
            }
            finally
            {
                FbxNative.FreeMeshBuffer(ref buffer);
            }
        }

        return CombineMeshesAndCacheTopology(meshes);
    }

    private MeshData BakeCombinedMeshWithCachedTopology(int animationIndex, double timeSeconds)
    {
        var triangles = _combinedTriangles!;
        var vertices = _combinedVertices!;
        var vertexOffsets = _meshVertexOffsets!;
        var vertexCounts = _meshVertexCounts!;
        var triangleCounts = _meshTriangleCounts!;

        for (var i = 0; i < vertexOffsets.Length; i++)
        {
            var bakeStatus = FbxNative.BakeMeshAtTime(
                _scene.DangerousGetHandle(),
                i,
                animationIndex,
                (float)timeSeconds,
                out var buffer);

            if (bakeStatus != 0)
            {
                throw new InvalidOperationException($"FBX native mesh bake failed for mesh {i} with status {bakeStatus}.");
            }

            try
            {
                if (buffer.VertexCount != vertexCounts[i] || buffer.TriangleCount != triangleCounts[i])
                {
                    ClearCachedTopology();
                    return BakeCombinedMesh(animationIndex, timeSeconds);
                }

                CopyVertices(buffer, vertices, vertexOffsets[i]);
            }
            finally
            {
                FbxNative.FreeMeshBuffer(ref buffer);
            }
        }

        return _combinedMesh ??= new MeshData(vertices, triangles);
    }

    private MeshData CombineMeshesAndCacheTopology(IReadOnlyList<MeshData> meshes)
    {
        var vertexCount = 0;
        var triangleCount = 0;
        foreach (var mesh in meshes)
        {
            vertexCount += mesh.Vertices.Count;
            triangleCount += mesh.Triangles.Count;
        }

        if (vertexCount == 0 || triangleCount == 0)
        {
            return MeshData.Empty;
        }

        var vertices = new MeshVertex[vertexCount];
        var triangles = new MeshTriangle[triangleCount];
        var vertexOffsets = new int[meshes.Count];
        var vertexCounts = new int[meshes.Count];
        var triangleCounts = new int[meshes.Count];
        var vertexOffset = 0;
        var triangleOffset = 0;

        for (var meshIndex = 0; meshIndex < meshes.Count; meshIndex++)
        {
            var mesh = meshes[meshIndex];
            vertexOffsets[meshIndex] = vertexOffset;
            vertexCounts[meshIndex] = mesh.Vertices.Count;
            triangleCounts[meshIndex] = mesh.Triangles.Count;

            for (var i = 0; i < mesh.Vertices.Count; i++)
            {
                vertices[vertexOffset + i] = mesh.Vertices[i];
            }

            for (var i = 0; i < mesh.Triangles.Count; i++)
            {
                var triangle = mesh.Triangles[i];
                triangles[triangleOffset + i] = new MeshTriangle(
                    triangle.A + vertexOffset,
                    triangle.B + vertexOffset,
                    triangle.C + vertexOffset);
            }

            vertexOffset += mesh.Vertices.Count;
            triangleOffset += mesh.Triangles.Count;
        }

        _meshVertexOffsets = vertexOffsets;
        _meshVertexCounts = vertexCounts;
        _meshTriangleCounts = triangleCounts;
        _combinedVertices = vertices;
        _combinedTriangles = triangles;
        _combinedMesh = new MeshData(vertices, triangles);
        _combinedVertexCount = vertexCount;

        return _combinedMesh;
    }

    private void ClearCachedTopology()
    {
        _meshVertexOffsets = null;
        _meshVertexCounts = null;
        _meshTriangleCounts = null;
        _combinedVertices = null;
        _combinedTriangles = null;
        _combinedMesh = null;
        _combinedVertexCount = 0;
    }

    private static unsafe void CopyVertices(FbxNativeMeshBuffer buffer, MeshVertex[] target, int targetOffset)
    {
        if (buffer.IsEmpty)
        {
            return;
        }

        var nativeVertices = new ReadOnlySpan<FbxNativeVertex>((void*)buffer.Vertices, buffer.VertexCount);
        for (var i = 0; i < nativeVertices.Length; i++)
        {
            var vertex = nativeVertices[i];
            target[targetOffset + i] = new MeshVertex(
                new Vector3(vertex.X, vertex.Y, vertex.Z),
                new Vector3(vertex.NormalX, vertex.NormalY, vertex.NormalZ));
        }
    }

    private static IReadOnlyList<AnimationClip> LoadAnimations(nint scene, FbxNativeSceneInfo info)
    {
        if (info.AnimationCount <= 0)
        {
            return [];
        }

        var animations = new List<AnimationClip>(info.AnimationCount);
        for (var i = 0; i < info.AnimationCount; i++)
        {
            var status = FbxNative.GetAnimationInfo(scene, i, out var animation);
            if (status != 0)
            {
                continue;
            }

            var duration = Math.Max(0, animation.TimeEnd - animation.TimeBegin);
            animations.Add(new AnimationClip(animation.GetName(i), duration, 0, []));
        }

        return animations;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _scene.Dispose();
    }
}
