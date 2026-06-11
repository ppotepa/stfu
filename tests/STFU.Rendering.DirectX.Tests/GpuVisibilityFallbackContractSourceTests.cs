using Xunit;

namespace STFU.Rendering.DirectX.Tests;

public sealed class GpuVisibilityFallbackContractSourceTests
{
    [Fact]
    public void DxGpuVisibilityBufferPass_ReportsCpuGpuVisibilityParity()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/runtime/STFU.Rendering.DirectX/Passes/DxGpuVisibilityBufferPass.cs",
            "VisibilityParityStats",
            "cpuVisibleCount",
            "gpuVisibleCount",
            "mismatches");
    }

    [Fact]
    public void DirectXBackend_UsesGpuVisibilityOnlyThroughQualityProfile()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/runtime/STFU.Rendering.DirectX/Backend/DirectXRenderBackend.cs",
            "UseGpuVisibilityBuffer",
            "VisibilityParity");
    }

    [Fact]
    public void QualityProfile_HasGpuVisibilityToggle()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/aot/STFU.Rendering.Abstractions/Requests/NprQualityProfile.cs",
            "UseGpuVisibilityBuffer");
    }

    [Fact]
    public void VisibilityProvider_ContractExistsWithoutDirectXTypes()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/aot/STFU.Abstractions/Visibility/IVisibilityBufferProvider.cs",
            "IVisibilityBufferProvider",
            "VisibilityBufferRequest",
            "VisibilityBufferResult");
    }

    [Fact]
    public void Dx11VisibilityProvider_DoesNotFakeGpuSuccess()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/runtime/STFU.Rendering.DirectX/Visibility/Dx11VisibilityBufferProvider.cs",
            "UsedGpu: false",
            "UsedFallback: true",
            "Array.Empty<int>()");
    }

    [Fact]
    public void DirectXModule_RegistersVisibilityProvider()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/runtime/STFU.Rendering.DirectX/DirectXRenderingModule.cs",
            "IVisibilityBufferProvider",
            "Dx11VisibilityBufferProvider");
    }

    [Fact]
    public void CpuReferenceVisibilityProvider_ExistsAsNeutralAdapter()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/aot/STFU.NPR/Visibility/CpuReferenceVisibilityBufferProvider.cs",
            "CpuReferenceVisibilityBufferProvider",
            "UsedFallback: true",
            "Array.Empty<int>()");
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
