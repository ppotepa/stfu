using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using STFU.Logging;

namespace STFU.Rendering.DirectX.Device;

public static class DirectXShaderCompiler
{
    private const uint D3dcCompileOptimizationLevel3 = 1u << 15;
    private const uint D3dcCompileDebug = 1u << 0;
    private const uint D3dcCompileSkipOptimization = 1u << 2;

    public static byte[] CompileFromFile(string relativePath, string entryPoint, string profile)
    {
        var path = ResolveShaderPath(relativePath);
        var flags = D3dcCompileOptimizationLevel3;
#if DEBUG
        flags |= D3dcCompileDebug | D3dcCompileSkipOptimization;
#endif

        var hr = D3DCompileFromFile(
            path,
            IntPtr.Zero,
            IntPtr.Zero,
            entryPoint,
            profile,
            flags,
            0,
            out var codeBlob,
            out var errorBlob);

        try
        {
            if (hr < 0 || codeBlob == IntPtr.Zero)
            {
                var message = errorBlob != IntPtr.Zero
                    ? ReadBlobAsString(errorBlob)
                    : $"Failed to compile shader '{relativePath}' ({entryPoint}, {profile}). HRESULT=0x{hr:X8}";
                StfuLog.Write(
                    StfuLogDomain.RenderGpu,
                    "shader.compile.failed",
                    message,
                    StfuLogLevel.Error,
                    new Dictionary<string, object?>
                    {
                        ["shader"] = relativePath,
                        ["entryPoint"] = entryPoint,
                        ["profile"] = profile
                    });
                throw new InvalidOperationException(message);
            }

            StfuLog.Write(
                StfuLogDomain.RenderGpu,
                "shader.compile.completed",
                relativePath,
                properties: new Dictionary<string, object?>
                {
                    ["entryPoint"] = entryPoint,
                    ["profile"] = profile
                });
            return ReadBlobBytes(codeBlob);
        }
        finally
        {
            ReleaseBlob(errorBlob);
            ReleaseBlob(codeBlob);
        }
    }

    private static string ResolveShaderPath(string relativePath)
    {
        var baseDir = Path.Combine(AppContext.BaseDirectory, "Shaders");
        var outputPath = Path.Combine(baseDir, relativePath);
        if (File.Exists(outputPath))
        {
            return outputPath;
        }

        var repoPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "src",
            "runtime",
            "STFU.Rendering.DirectX",
            "Shaders",
            relativePath));

        if (File.Exists(repoPath))
        {
            return repoPath;
        }

        var exception = new FileNotFoundException($"Shader file was not found: {relativePath}", outputPath);
        StfuLog.Write(StfuLogDomain.RenderGpu, "shader.missing", exception.Message, StfuLogLevel.Error, exception: exception);
        throw exception;
    }

    private static byte[] ReadBlobBytes(IntPtr blobPtr)
    {
        unsafe
        {
            var bufferPointer = GetBufferPointer(blobPtr);
            var size = checked((int)GetBufferSize(blobPtr));
            var bytes = new byte[size];
            Marshal.Copy(bufferPointer, bytes, 0, size);
            return bytes;
        }
    }

    private static string ReadBlobAsString(IntPtr blobPtr)
    {
        var bytes = ReadBlobBytes(blobPtr);
        return System.Text.Encoding.UTF8.GetString(bytes).TrimEnd('\0', '\r', '\n');
    }

    private static void ReleaseBlob(IntPtr blobPtr)
    {
        if (blobPtr != IntPtr.Zero)
        {
            Marshal.Release(blobPtr);
        }
    }

    private static unsafe IntPtr GetBufferPointer(IntPtr blobPtr)
    {
        var vtbl = *(IntPtr**)blobPtr;
        var fn = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr>)vtbl[3];
        return fn(blobPtr);
    }

    private static unsafe nuint GetBufferSize(IntPtr blobPtr)
    {
        var vtbl = *(IntPtr**)blobPtr;
        var fn = (delegate* unmanaged[Stdcall]<IntPtr, nuint>)vtbl[4];
        return fn(blobPtr);
    }

    [DllImport("d3dcompiler_47.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int D3DCompileFromFile(
        string pFileName,
        IntPtr pDefines,
        IntPtr pInclude,
        [MarshalAs(UnmanagedType.LPStr)] string pEntrypoint,
        [MarshalAs(UnmanagedType.LPStr)] string pTarget,
        uint flags1,
        uint flags2,
        out IntPtr ppCode,
        out IntPtr ppErrorMsgs);
}
