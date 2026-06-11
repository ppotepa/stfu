using System;
using System.IO;
using Xunit;

namespace STFU.NPR.Parity.Tests;

public sealed class NprHotStepCountersTests
{
    [Fact]
    public void ReferenceQuality_emits_hot_step_counters()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(repo, "src/aot/npr/pipelines/STFU.NPR.Pipelines.ReferenceQuality/ReferenceQuality/Steps/BuildProjectedTrianglesStep.cs", "BuildProjectedTrianglesStep.sourceMeshes", "BuildProjectedTrianglesStep.rejectedTriangles");
        AssertFileContains(repo, "src/aot/npr/pipelines/STFU.NPR.Pipelines.ReferenceQuality/ReferenceQuality/Steps/BuildMeshTopologyStep.cs", "BuildMeshTopologyStep.outputTopologyEdges", "BuildMeshTopologyStep.outputProjectedEdges");
        AssertFileContains(repo, "src/aot/npr/pipelines/STFU.NPR.Pipelines.ReferenceQuality/ReferenceQuality/Steps/DefaultBuildFaceIdVisibilityBufferStep.cs", "DefaultBuildFaceIdVisibilityBufferStep.maxRangeTileRefs", "DefaultBuildFaceIdVisibilityBufferStep.invalidRasterInfoCount");
        AssertFileContains(repo, "src/aot/npr/pipelines/STFU.NPR.Pipelines.ReferenceQuality/ReferenceQuality/Steps/DefaultBuildInkFrameStep.cs", "DefaultBuildInkFrameStep.layerIndexSilhouette", "DefaultBuildInkFrameStep.segmentScratchWaste");
        AssertFileContains(repo, "src/aot/npr/pipelines/STFU.NPR.Pipelines.ReferenceQuality/ReferenceQuality/Steps/DefaultBuildPathsFromFragmentsStep.cs", "DefaultBuildPathsFromFragmentsStep.fragmentsInput", "DefaultBuildPathsFromFragmentsStep.maxWalkPointCount");
        AssertFileContains(repo, "src/aot/npr/pipelines/STFU.NPR.Pipelines.ReferenceQuality/ReferenceQuality/Steps/DefaultClassifyEdgesToFragmentsStep.cs", "DefaultClassifyEdgesToFragmentsStep.earlyAccepted", "DefaultClassifyEdgesToFragmentsStep.endpointVisibilityTests");
        AssertFileContains(repo, "src/aot/STFU.Rendering.Cpu/Rasterization/CpuStrokeRasterizer.cs", "stroke.tileRefs", "stroke.maxRefsPerTile");
        AssertFileContains(repo, "src/runtime/STFU.Rendering.DirectX/Passes/DxToneSurfacePass.cs", "DxToneSurfacePass.cacheHits", "DxToneSurfacePass.cacheMisses");
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

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
