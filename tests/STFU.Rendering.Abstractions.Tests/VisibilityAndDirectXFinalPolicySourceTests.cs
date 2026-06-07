using Xunit;

namespace STFU.Rendering.Abstractions.Tests;

public sealed class VisibilityAndDirectXFinalPolicySourceTests
{
    [Fact]
    public void VisibilityParityPolicy_FailsClosedAndRecordsFallbackReason()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(repo, "src/aot/STFU.Rendering.Abstractions/Diagnostics/VisibilityParityStats.cs", "FallbackUsed", "FallbackReason", "ShouldFallback", "ToDiagnosticString");
        AssertFileContains(repo, "src/aot/STFU.Rendering.Abstractions/Requests/NprQualityProfile.cs", "UseGpuVisibilityBuffer");
    }

    [Fact]
    public void DirectXUploadAndReadbackPolicy_HasObservableCounters()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(repo, "src/runtime/STFU.Rendering.DirectX/Diagnostics/DirectXRenderCounters.cs", "StrokeInstancesBuilt", "StrokeInstanceUploads", "StrokeInstanceBufferRecreates", "Readbacks", "UploadedBytes");
        AssertFileContains(repo, "src/runtime/STFU.Rendering.DirectX/Passes/DxStrokeRasterPass.cs", "StrokeInstanceUploads", "StrokeInstanceBufferRecreates");
        AssertFileContains(repo, "src/runtime/STFU.Rendering.DirectX/Passes/DxReadbackPass.cs", "Readbacks");
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
            if (File.Exists(Path.Combine(directory.FullName, "STFU.slnx")) || Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root from test output directory.");
    }
}
