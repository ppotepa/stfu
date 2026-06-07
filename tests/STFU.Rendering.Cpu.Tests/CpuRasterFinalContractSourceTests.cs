using Xunit;

namespace STFU.Rendering.Cpu.Tests;

public sealed class CpuRasterFinalContractSourceTests
{
    [Fact]
    public void CpuRasterFinalContracts_AreImplementedInSource()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(repo, "src/aot/STFU.Rendering.Cpu/Rasterization/CpuRasterWorkspace.cs", "GetTiles", "RangeTileCounts", "TileSegmentIndices", "GetToneSourceXMap", "GetToneSourceYMap", "EnsureToneScratchCapacity");
        AssertFileContains(repo, "src/aot/STFU.Rendering.Cpu/Rasterization/CpuStrokeRasterizer.cs", "StrokeTileRefs", "StrokeTilesTouched", "StrokePixelTests", "StrokePixelWrites");
        AssertFileContains(repo, "src/aot/STFU.Rendering.Cpu/Rasterization/CpuToneRasterizer.cs", "ToneSameSizeFastPath", "TonePixels", "GetToneSourceXMap", "GetToneSourceYMap");
    }

    [Fact]
    public void CpuRasterFinalContracts_AreCoveredByRegressionTests()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(repo, "tests/STFU.Rendering.Cpu.Tests/CpuRasterWorkspaceFinalHardeningTests.cs", "GetTiles_ReusesCachedLayout_ForSameDimensionsAndTileSize", "ToneScratch_GrowsButDoesNotShrink");
        AssertFileContains(repo, "tests/STFU.Rendering.Cpu.Tests/CpuRasterOptimizationContractTests.cs", "StrokeSegmentsInput", "StrokeTileRefs", "TonePixels");
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
