namespace STFU.Rendering.Cpu.Tests;

using Xunit;

public sealed class CpuRasterOptimizationContractTests
{
    [Fact]
    public void CpuRasterWorkspace_SourceFile_ContainsTileAndToneReuseState()
    {
        var repo = FindRepositoryRoot();
        var path = Path.Combine(repo, "src", "aot", "STFU.Rendering.Cpu", "Rasterization", "CpuRasterWorkspace.cs");
        Assert.True(File.Exists(path), $"Missing expected file: {path}");

        var text = File.ReadAllText(path);
        Assert.Contains("RangeTileCounts", text, StringComparison.Ordinal);
        Assert.Contains("RangeTileOffsets", text, StringComparison.Ordinal);
        Assert.Contains("TileCounts", text, StringComparison.Ordinal);
        Assert.Contains("TileOffsets", text, StringComparison.Ordinal);
        Assert.Contains("TileSegmentIndices", text, StringComparison.Ordinal);
        Assert.Contains("GetTiles", text, StringComparison.Ordinal);
        Assert.Contains("ToneSourceX", text, StringComparison.Ordinal);
        Assert.Contains("ToneSourceY", text, StringComparison.Ordinal);
        Assert.Contains("ToneCoverageScratch", text, StringComparison.Ordinal);
        Assert.Contains("ToneAlphaScratch", text, StringComparison.Ordinal);
    }

    [Fact]
    public void CpuRasterizers_SourceFiles_ExposeFinalOptimizationCounters()
    {
        var repo = FindRepositoryRoot();
        AssertFileContains(repo, "src/aot/STFU.Rendering.Cpu/Rasterization/CpuStrokeRasterizer.cs", "StrokeSegmentsInput", "StrokeTileRefs", "StrokeTilesTouched", "StrokePixelTests", "StrokePixelWrites");
        AssertFileContains(repo, "src/aot/STFU.Rendering.Cpu/Rasterization/CpuToneRasterizer.cs", "TonePixels", "GetSourceXMap", "GetSourceYMap", "ToneSameSizeFastPath");
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
