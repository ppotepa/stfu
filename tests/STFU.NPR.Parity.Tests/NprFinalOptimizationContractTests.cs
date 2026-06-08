namespace STFU.NPR.Parity.Tests;

using Xunit;

public sealed class NprFinalOptimizationContractTests
{
    [Fact]
    public void FinalOptimization_SourceContracts_ArePresent()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(repo, "src/aot/npr/pipelines/STFU.NPR.Pipelines.ReferenceQuality/ReferenceQuality/Steps/DefaultBuildInkFrameStep.cs", "InkSegmentPlan", "_segmentEmitFlags", "_pathEmitOffsets");
        AssertFileContains(repo, "src/aot/npr/pipelines/STFU.NPR.Pipelines.ReferenceQuality/ReferenceQuality/Steps/DefaultBuildFaceIdVisibilityBufferStep.cs", "rangeTileCounts", "rangeTileOffsets", "pixelTests", "pixelWrites");
        AssertFileContains(repo, "src/aot/npr/pipelines/STFU.NPR.Pipelines.ReferenceQuality/ReferenceQuality/Steps/DefaultBuildPathsFromFragmentsStep.cs", "_silhouetteScratch", "_featureScratch", "_boundaryScratch");
        AssertFileContains(repo, "src/aot/npr/pipelines/STFU.NPR.Pipelines.ReferenceQuality/ReferenceQuality/Steps/DefaultSimplifyAndSortPathsStep.cs", "PathSimplificationMath");
        AssertFileContains(repo, "src/aot/STFU.Rendering.Abstractions/Diagnostics/VisibilityParityStats.cs", "FallbackUsed", "FallbackReason");
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
