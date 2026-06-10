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
        var fallbackIndex = text.IndexOf("_referenceFallback.Execute(context)", StringComparison.Ordinal);
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
    public void Interactive_frame_signature_hashes_transform_and_scene_role()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractiveFrameHasher.cs",
            "Mix(ulong hash, Transform3D value)",
            "value.Position",
            "value.Rotation",
            "value.Scale",
            "entity.Transform");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractiveFrameSignatureFactory.cs",
            "context.EntityStyles.DefaultRole",
            "context.EntityStyles.GetRole(entity.Id)");
    }

    [Fact]
    public void Viewport_request_factory_routes_selected_frame_pipeline_strategy()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/runtime/STFU.UI/Viewport/ViewportFramePipelineSelector.cs",
            "BuiltInFramePipelineStrategies.CreateRegistry",
            "FramePipelineStrategy.InteractivePerformance",
            "provider.CreatePipeline(options)",
            "UseReferenceFallbackForFinalFrame = true",
            "EnableInteractivePreviewOutput = false");

        AssertFileContains(
            repo,
            "src/runtime/STFU.UI/Viewport/ViewportRenderRequestFactory.cs",
            "_pipelineSelector.Select",
            "Pipeline: pipelineSelection.Pipeline",
            "ActivePipelineId: pipelineSelection.PipelineId",
            "PipelineStrategy: pipelineSelection.Strategy");
    }

    [Fact]
    public void Builtin_npr_pipeline_provider_list_includes_interactive_strategy_provider()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines/BuiltInNprPipelines.cs",
            "InteractivePerformancePipelineProvider",
            "new ReferenceQualityPipelineProvider()",
            "new InteractivePerformancePipelineProvider()",
            "new ComicSurfacePipelineProvider()");
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


    [Fact]
    public void Interactive_output_health_is_reported_to_diagnostics_counters()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractiveOutputHealthAnalyzer.cs",
            "InteractiveOutputHealthStatus",
            "ReturningReferenceFallback",
            "ReturningInteractivePreview");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/InteractivePerformanceNprPipeline.cs",
            "CaptureOutputHealth",
            "InteractiveOutputHealthAnalyzer.Analyze");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractiveDiagnosticsBridge.cs",
            "InteractivePerformance.outputHealthStatus",
            "InteractivePerformance.outputHealthScore",
            "InteractivePerformance.outputHealthWarningCount");

        AssertFileContains(
            repo,
            "src/runtime/STFU.UI/Viewport/ViewportRenderBridge.cs",
            "InteractivePerformance.outputHealthStatus",
            "FormatInteractiveOutputHealth",
            "Health {FormatInteractiveOutputHealth");
    }

    [Fact]
    public void Build_fix_contract_keeps_viewport_pipeline_selector_and_scene_panel_warning_free()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/runtime/STFU.UI/Viewport/ViewportFramePipelineSelector.cs",
            "using STFU.Rendering.Abstractions.Execution;",
            "NprRenderContentKind contentKind");

        var scenePanelPath = Path.Combine(repo, "src/runtime/STFU.UI.Bridge/Scene/ScenePanelViewModel.cs");
        Assert.True(File.Exists(scenePanelPath), $"Missing file: {scenePanelPath}");
        var scenePanel = File.ReadAllText(scenePanelPath);
        Assert.DoesNotContain("_suspendEntityCommit", scenePanel);
        Assert.Contains("if (_isRefreshing)", scenePanel);
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
            "ProjectionArtifactBuilder.BuildAll");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Stages/ProjectionArtifactBuilder.cs",
            "EnableSelfContainedProjection",
            "PreferSelfContainedProjection",
            "InteractiveProjectionScratchBuilder.Build");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Artifacts/ProjectedVertexArtifact.cs",
            "InteractiveProjectedVertex",
            "VisibleVertexCount",
            "VisibleVertexRatioPercent",
            "InteractiveProjectionSource");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Artifacts/ProjectedTriangleArtifact.cs",
            "InteractiveProjectedTriangle",
            "FrontFacingTriangleCount",
            "VisibleTriangleCount",
            "InteractiveProjectionSource");
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


    [Fact]
    public void Interactive_pipeline_builds_stroke_frame_from_visible_segments()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.Abstractions/FramePipelineStrategyOptions.cs",
            "EnableInteractiveStrokeFrameStage { get; init; } = true",
            "InteractivePreviewMaxStrokeSegments");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Stages/InteractiveFrameOrchestrator.cs",
            "EnableInteractiveStrokeFrameStage",
            "InteractiveStrokeFrameStage");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Stages/InteractiveStrokeFrameBuilder.cs",
            "BuildFrame",
            "StrokeSegmentPathList",
            "InteractivePerformance",
            "ResolveMaxSegments");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Artifacts/InteractiveStrokeFrameArtifact.cs",
            "StrokeFrame",
            "HasRenderableFrame",
            "StrokeFrameCoveragePercent");
    }

    [Fact]
    public void Interactive_pipeline_can_optionally_return_interactive_preview_frame()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/InteractivePerformanceNprPipeline.cs",
            "InteractivePreviewPolicy.TrySelectInteractiveFrame",
            "ReturnedInteractiveFrame",
            "ReturnedReferenceFallback",
            "UseReferenceFallbackForFinalFrame");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractivePreviewPolicy.cs",
            "EnableInteractivePreviewOutput",
            "RequireToneCoverageForInteractivePreview",
            "UseReferenceFallbackForFinalFrame");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractiveOutputSelector.cs",
            "InteractiveStrokeFrame",
            "InteractivePreviewCandidate",
            "HasRenderableFrame");
    }

    [Fact]
    public void Viewport_summary_reports_interactive_frame_and_output_source()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/runtime/STFU.UI/Viewport/ViewportRenderBridge.cs",
            "InteractivePerformance.interactiveStrokeFrameSegments",
            "Output interactive",
            "Output reference",
            "Frame {interactiveStrokeFramePaths");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractiveDiagnosticsBridge.cs",
            "InteractivePerformance.interactiveStrokeFramePaths",
            "InteractivePerformance.returnedInteractiveFrame",
            "InteractivePerformance.returnedReferenceFallback");
    }


    [Fact]
    public void Interactive_projection_and_visibility_can_run_from_self_contained_artifacts()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.Abstractions/FramePipelineStrategyOptions.cs",
            "EnableSelfContainedProjection",
            "PreferSelfContainedProjection",
            "EnableProjectedTriangleVisibility");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Stages/InteractiveProjectionScratchBuilder.cs",
            "ProjectMeshStep",
            "BuildProjectedTrianglesStep",
            "ScratchProjection");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Providers/ProjectedTriangleVisibilityProvider.cs",
            "BuildVisibleFaces",
            "ProjectedTriangles",
            "InteractiveVisibilitySource.ProjectedTriangles");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractiveDiagnosticsBridge.cs",
            "InteractivePerformance.projectionBuiltSelfContained",
            "InteractivePerformance.visibilityUsedProjectedTriangles");
    }



    [Fact]
    public void Interactive_preview_policy_exposes_decision_contract()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractivePreviewDecisionKind.cs",
            "SelectedInteractiveFrame",
            "ReferenceFallbackRequired",
            "PreviewOutputDisabled",
            "MissingToneCoverage");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractivePreviewPolicy.cs",
            "public static InteractivePreviewDecision Decide",
            "ForceReferenceFallback",
            "UseReferenceFallbackForFinalFrame",
            "EnableInteractivePreviewOutput",
            "RequireToneCoverageForInteractivePreview");
    }

    [Fact]
    public void Interactive_preview_output_is_environment_gated_for_viewport()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/runtime/STFU.UI/Viewport/ViewportInteractivePerformanceOptionsResolver.cs",
            "STFU_INTERACTIVE_PREVIEW_OUTPUT",
            "STFU_INTERACTIVE_FORCE_REFERENCE_FALLBACK",
            "STFU_INTERACTIVE_PREVIEW_REQUIRE_TONE",
            "STFU_INTERACTIVE_PREVIEW_MAX_SEGMENTS",
            "STFU_INTERACTIVE_PREFER_SELF_CONTAINED_PROJECTION");

        AssertFileContains(
            repo,
            "src/runtime/STFU.UI/Viewport/ViewportFramePipelineSelector.cs",
            "ViewportInteractivePerformanceOptionsResolver.Create",
            "ForceReferenceFallback",
            "EnableInteractiveOutputContract",
            "EnableProjectedTriangleVisibility");
    }

    [Fact]
    public void Interactive_preview_decision_is_written_to_diagnostics()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/InteractivePerformanceNprPipeline.cs",
            "InteractivePreviewPolicy.Decide",
            "result.Diagnostics.PreviewDecision",
            "ReturnedInteractiveFramePaths",
            "ReturnedInteractiveFrameSegments");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractiveDiagnosticsBridge.cs",
            "InteractivePerformance.previewDecision",
            "InteractivePerformance.returnedInteractiveFramePaths",
            "InteractivePerformance.returnedInteractiveFrameSegments");
    }



    [Fact]
    public void Interactive_output_selector_exposes_readiness_ladder()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractiveOutputReadiness.cs",
            "ProjectionReady",
            "VisibilityReady",
            "CandidateEdgesReady",
            "StrokeFrameReady",
            "PreviewReady");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractiveOutputSelector.cs",
            "ResolveReadiness",
            "ToReadinessScore",
            "InteractiveOutputReadiness.PreviewReady");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractiveDiagnosticsBridge.cs",
            "InteractivePerformance.outputReadiness",
            "InteractivePerformance.outputReadinessScore");
    }



    [Fact]
    public void Interactive_preview_output_gates_are_documented()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "docs/interactive-performance-preview-output.md",
            "STFU_INTERACTIVE_PREVIEW_OUTPUT",
            "STFU_INTERACTIVE_FORCE_REFERENCE_FALLBACK",
            "InteractivePreviewDecisionKind",
            "Readiness ladder");
    }



    [Fact]
    public void Interactive_artifact_store_pruning_contract_is_visible()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Artifacts/ArtifactStore.cs",
            "PruneFrameOrCameraArtifacts",
            "PruneFrameOrCameraArtifactsPerKind",
            "PruneTotalFrameOrCameraArtifacts");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Stages/InteractiveFrameOrchestrator.cs",
            "PruneArtifactStore",
            "MaxFrameOrCameraArtifactsPerKind",
            "MaxTotalFrameOrCameraArtifacts");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractiveDiagnosticsBridge.cs",
            "InteractivePerformance.prunedFrameOrCameraArtifacts");
    }

    [Fact]
    public void Interactive_artifact_store_pruning_options_are_exposed_to_viewport()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.Abstractions/FramePipelineStrategyOptions.cs",
            "MaxFrameOrCameraArtifactsPerKind",
            "MaxTotalFrameOrCameraArtifacts");

        AssertFileContains(
            repo,
            "src/runtime/STFU.UI/Viewport/ViewportInteractivePerformanceOptionsResolver.cs",
            "STFU_INTERACTIVE_MAX_FRAME_ARTIFACTS_PER_KIND",
            "STFU_INTERACTIVE_MAX_FRAME_ARTIFACTS_TOTAL");
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
