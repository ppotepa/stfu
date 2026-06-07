namespace STFU.Rendering.DirectX.Tests;

using Xunit;

public sealed class DirectXUploadAndReadbackContractTests
{
    [Fact]
    public void DirectXRenderCounters_SourceFile_ContainsUploadAndReadbackCounters()
    {
        var repo = FindRepositoryRoot();
        var path = Path.Combine(repo, "src", "runtime", "STFU.Rendering.DirectX", "Diagnostics", "DirectXRenderCounters.cs");
        Assert.True(File.Exists(path), $"Missing expected file: {path}");

        var text = File.ReadAllText(path);
        Assert.Contains("StrokeInstancesBuilt", text, StringComparison.Ordinal);
        Assert.Contains("StrokeInstanceUploads", text, StringComparison.Ordinal);
        Assert.Contains("StrokeInstanceBufferRecreates", text, StringComparison.Ordinal);
        Assert.Contains("UploadedBytes", text, StringComparison.Ordinal);
        Assert.Contains("Readbacks", text, StringComparison.Ordinal);
    }

    [Fact]
    public void DirectXStrokeUpload_SourceFiles_PreferSegmentBasedPathAndPersistentBuffers()
    {
        var repo = FindRepositoryRoot();
        AssertFileContains(repo, "src/runtime/STFU.Rendering.DirectX/Upload/DxStrokeInstanceBuilder.cs", "PathSortEntry", "sortScratch");
        AssertFileContains(repo, "src/runtime/STFU.Rendering.DirectX/Upload/DxStrokeInstanceBuilder.cs", "EstimatePathInstanceCapacity", "Create");
        AssertFileContains(repo, "src/runtime/STFU.Rendering.DirectX/Passes/DxStrokeRasterPass.cs", "StrokeInstanceUploads", "StrokeInstanceBufferRecreates");
    }

    private static void AssertFileContains(string repo, string relativePath, params string[] expected)
    {
        var path = Path.Combine(repo, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(path), $"Missing expected file: {relativePath}");
        var text = File.ReadAllText(path);
        foreach (var token in expected)
        {
            Assert.Contains(token, text, StringComparison.Ordinal);
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "STFU.slnx"))
                || Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root from test output directory.");
    }
}
