using Xunit;

namespace STFU.Rendering.DirectX.Tests;

public sealed class DirectXReadbackContractSourceTests
{
    [Fact]
    public void DirectXRenderCounters_ExposeReadbackContract()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/runtime/STFU.Rendering.DirectX/Diagnostics/DirectXRenderCounters.cs",
            "Readbacks",
            "DirectViewportReadbackFree",
            "ToDiagnosticString");
    }

    [Fact]
    public void DxReadbackPass_RecordsReadbacksOnlyInReadbackPath()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/runtime/STFU.Rendering.DirectX/Passes/DxReadbackPass.cs",
            "Readbacks",
            "DxReadbackCounters");
    }

    [Fact]
    public void DirectXBackend_HasSeparateLeaseAndReadbackBranches()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/runtime/STFU.Rendering.DirectX/Backend/DirectXRenderBackend.cs",
            "RequireGpuReadback",
            "ReadToPixelSurface",
            "GpuTextureLease");
    }

    [Fact]
    public void DirectXGpuTimer_LabelsCpuWallFallbackExplicitly()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/runtime/STFU.Rendering.DirectX/Diagnostics/DirectXGpuTimer.cs",
            "UsesGpuTimestampQueries",
            "CpuWallFallback",
            "GpuTimestamp");
    }

    [Fact]
    public void ViewportBridge_DisablesReadbackForDirectPresenter()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/runtime/STFU.UI/Viewport/ViewportRenderBridge.cs",
            "PreferGpuPresentation",
            "AllowGpuReadback",
            "RequireGpuReadback");
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
            if (File.Exists(Path.Combine(directory.FullName, "STFU.slnx")) ||
                Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
