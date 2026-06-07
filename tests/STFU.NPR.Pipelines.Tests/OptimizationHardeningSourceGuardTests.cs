using System.Text.RegularExpressions;
using Xunit;

namespace STFU.NPR.Pipelines.Tests;

public sealed class OptimizationHardeningSourceGuardTests
{
    [Fact]
    public void HotPathFiles_DoNotReintroduceObviousLinqAllocationPatterns()
    {
        var repo = FindRepositoryRoot();
        var hotPathFiles = new[]
        {
            "src/aot/npr/pipelines/STFU.NPR.Pipelines/Default/Steps/DefaultBuildInkFrameStep.cs",
            "src/aot/npr/pipelines/STFU.NPR.Pipelines/Default/Steps/DefaultBuildFaceIdVisibilityBufferStep.cs",
            "src/aot/npr/pipelines/STFU.NPR.Pipelines/Default/Steps/DefaultClassifyEdgesToFragmentsStep.cs",
            "src/aot/npr/pipelines/STFU.NPR.Pipelines/Default/Steps/DefaultBuildPathsFromFragmentsStep.cs",
            "src/aot/npr/pipelines/STFU.NPR.Pipelines/Default/Steps/DefaultSimplifyAndSortPathsStep.cs",
            "src/aot/STFU.Rendering.Cpu/Rasterization/CpuStrokeRasterizer.cs",
            "src/aot/STFU.Rendering.Cpu/Rasterization/CpuToneRasterizer.cs",
            "src/runtime/STFU.Rendering.DirectX/Upload/DxStrokeInstanceBuilder.cs"
        };

        var forbidden = new[]
        {
            ".Select(",
            ".Where(",
            ".GroupBy(",
            ".OrderBy(",
            ".ToList(",
            ".ToArray("
        };

        var failures = new List<string>();
        foreach (var relativePath in hotPathFiles)
        {
            var path = Path.Combine(repo, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                continue;
            }

            var lines = File.ReadAllLines(path);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.Contains("HOTPATH-GUARD:ALLOW", StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (var pattern in forbidden)
                {
                    if (line.Contains(pattern, StringComparison.Ordinal))
                    {
                        failures.Add($"{relativePath}:{i + 1}: contains {pattern}");
                    }
                }
            }
        }

        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    [Fact]
    public void OptimizedPipelineFiles_ExposeExpectedCountersAndScratchContracts()
    {
        var repo = FindRepositoryRoot();
        AssertFileContains(repo, "src/aot/npr/pipelines/STFU.NPR.Pipelines/Default/Steps/DefaultBuildInkFrameStep.cs", "_segmentEmitFlags", "_pathEmitOffsets", "precomputedPointCapacity");
        AssertFileContains(repo, "src/aot/npr/pipelines/STFU.NPR.Pipelines/Default/Steps/DefaultBuildFaceIdVisibilityBufferStep.cs", "pixelTests", "pixelWrites", "visibleFaces");
        AssertFileContains(repo, "src/aot/npr/pipelines/STFU.NPR.Pipelines/Default/Steps/DefaultBuildPathsFromFragmentsStep.cs", "PathBuildScratch", "expectedKindCapacity");
        AssertFileContains(repo, "src/aot/STFU.Rendering.Cpu/Rasterization/CpuRasterWorkspace.cs", "ToneSourceX", "ToneSourceY", "ToneCoverageScratch", "ToneAlphaScratch");
        AssertFileContains(repo, "src/aot/STFU.Rendering.Cpu/Rasterization/CpuStrokeRasterizer.cs", "TileSegmentIndices", "StrokeTileRefs");
        AssertFileContains(repo, "src/aot/STFU.Rendering.Cpu/Rasterization/CpuToneRasterizer.cs", "ToneSameSizeFastPath");
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
