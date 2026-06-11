using Xunit;

namespace STFU.NPR.Pipelines.Tests;

public sealed class InteractiveSelfContainedProjectionInputTests
{
    [Fact]
    public void Scratch_projection_builder_does_not_read_reference_graph_directly()
    {
        var repo = FindRepositoryRoot();
        var path = Path.Combine(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Stages/InteractiveProjectionScratchBuilder.cs");

        Assert.True(File.Exists(path), $"Missing file: {path}");
        var text = File.ReadAllText(path);

        Assert.DoesNotContain("sourceGraph = context.ReferenceContext.Graph", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ProjectMeshStep(sourceGraph)", text, StringComparison.Ordinal);
        Assert.Contains("InteractiveProjectionInputBuilder.Build", text, StringComparison.Ordinal);
        Assert.Contains("InteractiveProjectionGraphBuilder.Build", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Projection_snapshot_tracks_reference_graph_usage()
    {
        var repo = FindRepositoryRoot();
        var path = Path.Combine(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Stages/InteractiveProjectionSnapshot.cs");

        Assert.True(File.Exists(path), $"Missing file: {path}");
        var text = File.ReadAllText(path);

        Assert.Contains("UsedReferenceGraph", text, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "STFU.slnx")))
        {
            current = current.Parent;
        }

        return current?.FullName ?? throw new InvalidOperationException("Could not locate repository root.");
    }
}
