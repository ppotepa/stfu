using Xunit;

namespace STFU.NPR.Pipelines.Tests;

public sealed class InteractivePerformanceSourceContractTests
{
    [Fact]
    public void Interactive_performance_has_diagnostics_bridge()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractiveDiagnosticsBridge.cs",
            "WriteToContext",
            "InteractiveFrameDiagnostics");
    }

    [Fact]
    public void Interactive_orchestrator_knows_future_stage_flags()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Stages/InteractiveFrameOrchestrator.cs",
            "EnableVisibilityStage",
            "EnableCandidateEdgeStage",
            "EnableStrokePlanningStage",
            "EnableTonePlanningStage");
    }

    [Fact]
    public void Request_carries_pipeline_strategy()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/aot/STFU.Rendering.Abstractions/Requests/NprRenderRequest.cs",
            "FramePipelineStrategy PipelineStrategy",
            "FramePipelineStrategy.ReferenceQuality");
    }

    private static void AssertFileContains(string repo, string relativePath, params string[] expected)
    {
        var path = Path.Combine(repo, relativePath);
        Assert.True(File.Exists(path), $"Missing file: {relativePath}");
        var text = File.ReadAllText(path);

        foreach (var value in expected)
        {
            Assert.Contains(value, text);
        }
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

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
