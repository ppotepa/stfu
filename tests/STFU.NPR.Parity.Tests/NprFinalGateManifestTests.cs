using Xunit;

namespace STFU.NPR.Parity.Tests;

public sealed class NprFinalGateManifestTests
{
    [Fact]
    public void FinalOptimizationGate_SourceContractsRemainPresent()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(repo, "src/aot/npr/pipelines/STFU.NPR.Pipelines/Default/Steps/DefaultBuildInkFrameStep.cs", "InkSegmentPlan", "_segmentEmitFlags", "_pathEmitOffsets", "_segmentScratch");
        AssertFileContains(repo, "src/aot/STFU.Rendering.Cpu/Rasterization/CpuRasterWorkspace.cs", "RangeTileCounts", "RangeTileOffsets", "TileSegmentIndices", "ToneSourceXMap", "ToneSourceYMap", "ToneCoverageScratch", "ToneAlphaScratch");
        AssertFileContains(repo, "src/aot/STFU.Rendering.Cpu/Rasterization/CpuStrokeRasterizer.cs", "StrokeSegmentsInput", "StrokeTileRefs", "StrokeTilesTouched", "StrokePixelTests", "StrokePixelWrites");
        AssertFileContains(repo, "src/aot/STFU.Rendering.Cpu/Rasterization/CpuToneRasterizer.cs", "GetToneSourceXMap", "GetToneSourceYMap", "ToneSameSizeFastPath", "TonePixels");
        AssertFileContains(repo, "src/aot/STFU.Rendering.Abstractions/Diagnostics/VisibilityParityStats.cs", "FallbackUsed", "FallbackReason", "ShouldFallback");
        AssertFileContains(repo, "src/runtime/STFU.Rendering.DirectX/Diagnostics/DirectXRenderCounters.cs", "StrokeInstanceUploads", "Readbacks", "UploadedBytes");
    }

    [Fact]
    public void FinalOptimizationGate_ValidationScriptsArePresent()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(repo, "tools/ci/run-npr-final-gate.ps1", "run-npr-final-optimization-validation.ps1", "run-npr-hotpath-audit.ps1");
        AssertFileContains(repo, "tools/ci/run-npr-final-optimization-validation.ps1", "run-render-sweep.ps1");
        AssertFileContains(repo, "tools/ci/run-render-sweep.ps1", "--workers", "--tile-size", "--render-optimizer");
        AssertFileContains(repo, "tools/ci/guard-parallelism.ps1", "pattern = '(?<!Deterministic)Parallel\\.(For|ForEach|Invoke)'", "$threadAuditPattern");
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
