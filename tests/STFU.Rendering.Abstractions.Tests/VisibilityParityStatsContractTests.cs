using Xunit;

namespace STFU.Rendering.Abstractions.Tests;

public sealed class VisibilityParityStatsContractTests
{
    [Fact]
    public void VisibilityParityStats_SourceFile_DefinesFallbackAndMismatchFields()
    {
        var repo = FindRepositoryRoot();
        var path = Path.Combine(repo, "src", "aot", "STFU.Rendering.Abstractions", "Diagnostics", "VisibilityParityStats.cs");
        Assert.True(File.Exists(path), $"Missing expected file: {path}");

        var text = File.ReadAllText(path);
        Assert.Contains("CpuVisibleFaces", text, StringComparison.Ordinal);
        Assert.Contains("GpuVisibleFaces", text, StringComparison.Ordinal);
        Assert.Contains("MissingOnGpu", text, StringComparison.Ordinal);
        Assert.Contains("ExtraOnGpu", text, StringComparison.Ordinal);
        Assert.Contains("MismatchCount", text, StringComparison.Ordinal);
        Assert.Contains("FallbackUsed", text, StringComparison.Ordinal);
        Assert.Contains("FallbackReason", text, StringComparison.Ordinal);
    }

    [Fact]
    public void NprQualityProfile_KeepsGpuVisibilityOptIn()
    {
        var repo = FindRepositoryRoot();
        var path = Path.Combine(repo, "src", "aot", "STFU.Rendering.Abstractions", "Requests", "NprQualityProfile.cs");
        Assert.True(File.Exists(path), $"Missing expected file: {path}");

        var text = File.ReadAllText(path);
        Assert.Contains("UseGpuVisibilityBuffer", text, StringComparison.Ordinal);
        Assert.Matches(@"UseGpuVisibilityBuffer\s*=\s*false", text);
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
