using Xunit;

namespace STFU.NPR.Parity.Tests;

public sealed class NprDeterminismReleaseGateTests
{
    [Fact]
    public void WorkerParityTests_CoverReleaseWorkerMatrix()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "tests/STFU.NPR.Parity.Tests/NprPipelineWorkerParityTests.cs",
            "workers",
            "1",
            "2",
            "4",
            "8",
            "16");
    }

    [Fact]
    public void HotPathAllocationGuard_CoversRendererCriticalFiles()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "tests/STFU.NPR.Parity.Tests/NprHotPathAllocationGuardTests.cs",
            "DefaultBuildInkFrameStep.cs",
            "DefaultBuildFaceIdVisibilityBufferStep.cs",
            "CpuStrokeRasterizer.cs",
            "CpuToneRasterizer.cs",
            "DxStrokeInstanceBuilder.cs");
    }

    [Fact]
    public void ParallelismTests_ValidateStableRangePartitioning()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "tests/STFU.Parallelism.Tests/DeterministicParallelCancellationTests.cs",
            "ForRanges_CancellationOverload_PreservesStableRanges",
            "DeterministicParallel.GetRangeCount",
            "ParallelRange");
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
