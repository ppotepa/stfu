using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using STFU.Logging;
using STFU.Mesh;

namespace STFU.Import.Fbx;

internal static partial class FbxNative
{
    private const string LibraryName = "stfu_fbx";
    private static int _loggedNativeLibraryPath;
    private static int _loggedNativeLibraryMissing;

    static FbxNative()
    {
        try
        {
            NativeLibrary.SetDllImportResolver(typeof(FbxNative).Assembly, ResolveNativeLibrary);
        }
        catch (InvalidOperationException)
        {
            // Another resolver for this assembly is already installed.
        }
    }

    private static IntPtr ResolveNativeLibrary(
        string libraryName,
        Assembly assembly,
        DllImportSearchPath? searchPath)
    {
        if (!string.Equals(libraryName, LibraryName, StringComparison.Ordinal))
        {
            return IntPtr.Zero;
        }

        foreach (var candidate in EnumerateNativeLibraryCandidates())
        {
            if (File.Exists(candidate) && NativeLibrary.TryLoad(candidate, out var handle))
            {
                if (Interlocked.Exchange(ref _loggedNativeLibraryPath, 1) == 0)
                {
                    StfuLog.Write(
                        StfuLogDomain.ImportFbx,
                        "native.loaded",
                        candidate);
                }

                return handle;
            }
        }

        if (Interlocked.Exchange(ref _loggedNativeLibraryMissing, 1) == 0)
        {
            StfuLog.Write(
                StfuLogDomain.ImportFbx,
                "native.missing",
                "stfu_fbx native library could not be resolved.",
                StfuLogLevel.Warning);
        }

        return IntPtr.Zero;
    }

    private static IEnumerable<string> EnumerateNativeLibraryCandidates()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "stfu_fbx.dll");
        yield return Path.Combine(Environment.CurrentDirectory, "stfu_fbx.dll");

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            yield return Path.Combine(directory.FullName, "artifacts", "native", "STFU.Native.Fbx", "stfu_fbx.dll");
            directory = directory.Parent;
        }
    }

    [LibraryImport(LibraryName, EntryPoint = "stfu_fbx_load", StringMarshalling = StringMarshalling.Utf8)]
    internal static partial nint Load(string path, out FbxNativeError error);

    [LibraryImport(LibraryName, EntryPoint = "stfu_fbx_free")]
    internal static partial void Free(nint scene);

    [LibraryImport(LibraryName, EntryPoint = "stfu_fbx_get_scene_info")]
    internal static partial int GetSceneInfo(nint scene, out FbxNativeSceneInfo info);

    [LibraryImport(LibraryName, EntryPoint = "stfu_fbx_get_bone_info")]
    internal static partial int GetBoneInfo(nint scene, int boneIndex, out FbxNativeBoneInfo info);

    [LibraryImport(LibraryName, EntryPoint = "stfu_fbx_get_animation_info")]
    internal static partial int GetAnimationInfo(nint scene, int animationIndex, out FbxNativeAnimationInfo info);

    [LibraryImport(LibraryName, EntryPoint = "stfu_fbx_bake_mesh_at_time")]
    internal static partial int BakeMeshAtTime(
        nint scene,
        int meshIndex,
        int animationIndex,
        float timeSeconds,
        out FbxNativeMeshBuffer buffer);

    [LibraryImport(LibraryName, EntryPoint = "stfu_fbx_free_mesh_buffer")]
    internal static partial void FreeMeshBuffer(ref FbxNativeMeshBuffer buffer);
}

[StructLayout(LayoutKind.Sequential)]
internal struct FbxNativeError
{
    public int Code;
    public nint Message;

    public readonly string GetMessage()
    {
        return Message == 0
            ? $"FBX native error {Code}."
            : Marshal.PtrToStringUTF8(Message) ?? $"FBX native error {Code}.";
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct FbxNativeSceneInfo
{
    public int MeshCount;
    public int SkinnedMeshCount;
    public int SkeletonCount;
    public int AnimationCount;
}

[StructLayout(LayoutKind.Sequential)]
internal struct FbxNativeBoneInfo
{
    public int ParentIndex;
    public nint Name;

    public readonly string GetName(int fallbackIndex)
    {
        return Name == 0
            ? $"bone_{fallbackIndex}"
            : Marshal.PtrToStringUTF8(Name) ?? $"bone_{fallbackIndex}";
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct FbxNativeAnimationInfo
{
    public double TimeBegin;
    public double TimeEnd;
    public nint Name;

    public readonly string GetName(int fallbackIndex)
    {
        return Name == 0
            ? $"animation_{fallbackIndex}"
            : Marshal.PtrToStringUTF8(Name) ?? $"animation_{fallbackIndex}";
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct FbxNativeVertex
{
    public float X;
    public float Y;
    public float Z;
    public float NormalX;
    public float NormalY;
    public float NormalZ;
}

[StructLayout(LayoutKind.Sequential)]
internal struct FbxNativeTriangle
{
    public int A;
    public int B;
    public int C;
}

[StructLayout(LayoutKind.Sequential)]
internal struct FbxNativeMeshBuffer
{
    public int VertexCount;
    public int TriangleCount;
    public nint Vertices;
    public nint Triangles;

    public readonly bool IsEmpty => VertexCount <= 0 || TriangleCount <= 0 || Vertices == 0 || Triangles == 0;

    public readonly unsafe MeshData ToMeshData()
    {
        if (IsEmpty)
        {
            return MeshData.Empty;
        }

        var nativeVertices = new ReadOnlySpan<FbxNativeVertex>((void*)Vertices, VertexCount);
        var nativeTriangles = new ReadOnlySpan<FbxNativeTriangle>((void*)Triangles, TriangleCount);
        var vertices = new MeshVertex[VertexCount];
        var triangles = new MeshTriangle[TriangleCount];

        for (var i = 0; i < nativeVertices.Length; i++)
        {
            var vertex = nativeVertices[i];
            vertices[i] = new MeshVertex(
                new Vector3(vertex.X, vertex.Y, vertex.Z),
                new Vector3(vertex.NormalX, vertex.NormalY, vertex.NormalZ));
        }

        for (var i = 0; i < nativeTriangles.Length; i++)
        {
            var triangle = nativeTriangles[i];
            triangles[i] = new MeshTriangle(triangle.A, triangle.B, triangle.C);
        }

        return new MeshData(vertices, triangles);
    }
}
