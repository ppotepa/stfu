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
    public void Interactive_default_options_enable_stroke_and_tone_artifact_planning()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.Abstractions/FramePipelineStrategyOptions.cs",
            "EnableStrokePlanningStage { get; init; } = true",
            "EnableTonePlanningStage { get; init; } = true");
    }

    [Fact]
    public void Interactive_orchestrator_tracks_frame_signatures_and_cache_stats()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Stages/InteractiveFrameOrchestrator.cs",
            "InteractiveFrameChangeTracker",
            "AdaptiveBudgetController",
            "CaptureArtifactStoreStats",
            "SnapshotStats");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractiveDiagnosticsBridge.cs",
            "InteractivePerformance.workClass",
            "InteractivePerformance.qualityMode",
            "InteractivePerformance.artifactStoreItems",
            "InteractivePerformance.cameraHash");
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
    public void Interactive_pipeline_harvests_artifacts_after_reference_fallback()
    {
        var repo = FindRepositoryRoot();
        var path = Path.Combine(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/InteractivePerformanceNprPipeline.cs");
        Assert.True(File.Exists(path), $"Missing file: {path}");

        var text = File.ReadAllText(path);
        var fallbackIndex = text.IndexOf("var frame = _referenceFallback.Execute(context);", StringComparison.Ordinal);
        var orchestratorIndex = text.IndexOf("var result = _orchestrator.Execute(intent, context);", StringComparison.Ordinal);

        Assert.True(fallbackIndex >= 0, "Reference fallback execution was not found.");
        Assert.True(orchestratorIndex > fallbackIndex, "Interactive artifact harvest must run after Reference Quality populates the graph.");
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
            "InteractivePerformance.candidateEdges",
            "InteractivePerformance.strokeCommands");

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
            "Edges",
            "Strokes",
            "Tones",
            "Work",
            "Cache");
    }


    [Fact]
    public void Interactive_artifact_keys_use_frame_signatures()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Artifacts/ArtifactKeyFactory.cs",
            "ProjectionSummary",
            "VisibleFaces",
            "CandidateEdges",
            "StrokeCommands",
            "ToneCoverage",
            "ResolveCameraHash");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractiveFrameSignatureFactory.cs",
            "ComputeContentHash",
            "ComputeCameraHash",
            "ComputeStyleHash",
            "ComputeViewportHash");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Stages/ProjectionStage.cs",
            "ArtifactKeyFactory.ProjectionSummary");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Stages/VisibilityStage.cs",
            "ArtifactKeyFactory.VisibleFaces");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Stages/CandidateEdgeStage.cs",
            "ArtifactKeyFactory.CandidateEdges");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Stages/StrokePlanningStage.cs",
            "ArtifactKeyFactory.StrokeCommands");
    }

    [Fact]
    public void Interactive_stroke_planning_builds_commands_from_candidate_edges()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Stages/StrokePlanningStage.cs",
            "LoadCandidateEdges",
            "StrokeCommandPlanner.BuildCommands",
            "TotalStrokeCandidates",
            "StrokeCommandReductionPercent");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Stages/StrokeCommandPlanner.cs",
            "BuildCommands",
            "InteractiveStrokeCommand",
            "ProjectedLength");
    }

    [Fact]
    public void Interactive_tone_planning_builds_visible_face_regions()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Stages/TonePlanningStage.cs",
            "LoadVisibleFaceSet",
            "ToneCoveragePlanner.BuildCoverage",
            "ToneSourceFaces",
            "ToneCoverageRatioPercent");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Stages/ToneCoveragePlanner.cs",
            "BuildRegions",
            "InteractiveToneRegion",
            "ResolveMaxRegions");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractiveDiagnosticsBridge.cs",
            "InteractivePerformance.toneSourceFaces",
            "InteractivePerformance.toneRegions",
            "InteractivePerformance.toneCoverageRatioPercent");
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


    [Fact]
    public void Interactive_projection_stage_builds_full_projected_geometry_artifacts()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Stages/ProjectionStage.cs",
            "ArtifactKeyFactory.ProjectedVertices",
            "ArtifactKeyFactory.ProjectedTriangles",
            "ProjectionArtifactBuilder.BuildVertices",
            "ProjectionArtifactBuilder.BuildTriangles");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Artifacts/ProjectedVertexArtifact.cs",
            "InteractiveProjectedVertex",
            "VisibleVertexCount",
            "VisibleVertexRatioPercent");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Artifacts/ProjectedTriangleArtifact.cs",
            "InteractiveProjectedTriangle",
            "FrontFacingTriangleCount",
            "VisibleTriangleCount");
    }

    [Fact]
    public void Interactive_pipeline_builds_visible_stroke_segment_artifact()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.Abstractions/FramePipelineStrategyOptions.cs",
            "EnableVisibleStrokeSegmentStage { get; init; } = true");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Stages/InteractiveFrameOrchestrator.cs",
            "EnableVisibleStrokeSegmentStage",
            "VisibleStrokeSegmentStage");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Stages/VisibleStrokeSegmentStage.cs",
            "VisibleStrokeSegmentPlanner.BuildSegments",
            "VisibleSegments",
            "VisibleSegmentCoveragePercent");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Artifacts/ArtifactKeyFactory.cs",
            "ProjectedVertices",
            "ProjectedTriangles",
            "VisibleStrokeSegments");
    }

    [Fact]
    public void Interactive_validation_script_is_available_for_rpack_actions()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "scripts/validate-interactive-performance.ps1",
            "dotnet build STFU.slnx",
            "--filter Interactive",
            "artifacts/rpack-validation/interactive-performance");
    }

    [Fact]
    public void Interactive_output_contract_is_declared_and_reported()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractivePipelineResult.cs",
            "InteractiveOutputSelection",
            "OutputKind",
            "HasInteractivePreviewCandidate");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractiveOutputSelector.cs",
            "InteractivePreviewCandidate",
            "VisibleStrokeSegments",
            "ToneCoverage");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.Abstractions/FramePipelineStrategyOptions.cs",
            "EnableInteractiveOutputContract",
            "UseReferenceFallbackForFinalFrame");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractiveDiagnosticsBridge.cs",
            "InteractivePerformance.outputKind",
            "InteractivePerformance.interactivePreviewCandidate",
            "InteractivePerformance.outputVisibleStrokeSegments");
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
