using Xunit;

namespace STFU.NPR.Parity.Tests;

public sealed class NprHotPathAllocationGuardTests
{
    private static readonly string[] HotPathFiles =
    [
        "src/aot/npr/pipelines/STFU.NPR.Pipelines/Default/Steps/DefaultBuildInkFrameStep.cs",
        "src/aot/npr/pipelines/STFU.NPR.Pipelines/Default/Steps/DefaultBuildFaceIdVisibilityBufferStep.cs",
        "src/aot/npr/pipelines/STFU.NPR.Pipelines/Default/Steps/DefaultClassifyEdgesToFragmentsStep.cs",
        "src/aot/npr/pipelines/STFU.NPR.Pipelines/Default/Steps/DefaultBuildPathsFromFragmentsStep.cs",
        "src/aot/npr/pipelines/STFU.NPR.Pipelines/Default/Steps/DefaultSimplifyAndSortPathsStep.cs",
        "src/aot/STFU.Rendering.Cpu/Rasterization/CpuStrokeRasterizer.cs",
        "src/aot/STFU.Rendering.Cpu/Rasterization/CpuToneRasterizer.cs",
        "src/runtime/STFU.Rendering.DirectX/Upload/DxStrokeInstanceBuilder.cs"
    ];

    private static readonly string[] GuardedTokens =
    [
        ".ToList(",
        ".Select(",
        ".Where(",
        ".GroupBy(",
        ".OrderBy(",
        ".OrderByDescending(",
        ".ThenBy(",
        ".ThenByDescending("
    ];

    [Fact]
    public void RendererHotPathFiles_DoNotUseCommonLinqAllocationOperators()
    {
        var repoRoot = FindRepositoryRoot();
        var offenders = new List<string>();

        foreach (var relativePath in HotPathFiles)
        {
            var fullPath = Path.Combine(repoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(fullPath))
            {
                continue;
            }

            var lines = File.ReadAllLines(fullPath);
            for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                var line = lines[lineIndex];
                if (line.Contains("HOTPATH-GUARD:ALLOW", StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (var token in GuardedTokens)
                {
                    if (line.Contains(token, StringComparison.Ordinal))
                    {
                        offenders.Add($"{relativePath}:{lineIndex + 1}: {token}");
                    }
                }
            }
        }

        Assert.True(offenders.Count == 0, "Hot-path allocation guard found forbidden operators:" + Environment.NewLine + string.Join(Environment.NewLine, offenders));
    }

    [Fact]
    public void DefaultBuildDebugFrameStep_DoesNotUsePredicateCountForVisibilityTotals()
    {
        var repoRoot = FindRepositoryRoot();
        var path = Path.Combine(repoRoot, "src/aot/npr/pipelines/STFU.NPR.Pipelines/Default/Steps/DefaultBuildDebugFrameStep.cs".Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path))
        {
            return;
        }

        var text = File.ReadAllText(path);
        Assert.DoesNotContain("VisibilitySegments.Count(segment =>", text, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "STFU.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
