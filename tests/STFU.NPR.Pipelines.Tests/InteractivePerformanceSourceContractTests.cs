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

    [Fact]
    public void Viewport_hud_surfaces_active_pipeline_strategy()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/runtime/STFU.UI/Viewport/EngineViewportControl.cs",
            "DrawPipelineStatusHud",
            "Pipeline:",
            "FramePipelineStrategyDisplay.GetDisplayName",
            "output fallback: Reference Quality");

        AssertFileContains(
            repo,
            "src/runtime/STFU.UI/Viewport/ViewportRenderBridge.cs",
            "pipelineStrategy",
            "pipelineStrategyLabel",
            "pipelineStrategyStatus");
    }


    [Fact]
    public void Viewport_hud_uses_avalonia_formatted_text_width_height()
    {
        var repo = FindRepositoryRoot();
        var path = Path.Combine(repo, "src/runtime/STFU.UI/Viewport/EngineViewportControl.cs");
        Assert.True(File.Exists(path), $"Missing file: {path}");

        var text = File.ReadAllText(path);

        Assert.Contains("formattedText.Width", text);
        Assert.Contains("formattedText.Height", text);
        Assert.DoesNotContain("formattedText.Bounds", text);
    }

    [Fact]
    public void Interactive_performance_exposes_visibility_and_candidate_counters()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractiveDiagnosticsBridge.cs",
            "InteractivePerformance.totalFaces",
            "InteractivePerformance.visibleFaces",
            "InteractivePerformance.totalEdges",
            "InteractivePerformance.candidateEdges");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Providers/CpuReferenceVisibilityProvider.cs",
            "CpuReferenceVisibility",
            "DefaultFaceIdVisibility");
    }

    [Fact]
    public void Viewport_status_formats_interactive_pipeline_reduction_summary()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/runtime/STFU.UI/Viewport/ViewportRenderBridge.cs",
            "FormatInteractivePipelineSummary",
            "Faces",
            "Edges");
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
