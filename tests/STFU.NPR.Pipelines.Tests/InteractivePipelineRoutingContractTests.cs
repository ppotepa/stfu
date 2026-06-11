using Xunit;

namespace STFU.NPR.Pipelines.Tests;

public sealed class InteractivePipelineRoutingContractTests
{
    [Fact]
    public void Builtin_pipeline_provider_list_exposes_interactive_performance_without_adding_default_presets()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines/BuiltInNprPipelines.cs",
            "using STFU.NPR.Pipeline.InteractivePerformance;",
            "new ReferenceQualityPipelineProvider()",
            "new InteractivePerformancePipelineProvider()",
            "new ComicSurfacePipelineProvider()");

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/InteractivePerformancePipelineProvider.cs",
            "public string PipelineId => NprPipelineIds.InteractivePerformance",
            "public IReadOnlyList<INprPreset> CreateBuiltInPresets()",
            "return [];");
    }

    [Fact]
    public void Viewport_pipeline_selector_is_the_single_runtime_strategy_bridge()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/runtime/STFU.UI/Viewport/ViewportFramePipelineSelector.cs",
            "internal sealed class ViewportFramePipelineSelector",
            "BuiltInFramePipelineStrategies.CreateRegistry",
            "ViewportFramePipelineSelection Select",
            "NprRenderContentKind.NprPipeline",
            "FramePipelineStrategy.ReferenceQuality",
            "provider.CreatePipeline(options)");
    }

    [Fact]
    public void Viewport_pipeline_selector_keeps_reference_quality_on_active_preset_pipeline()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/runtime/STFU.UI/Viewport/ViewportFramePipelineSelector.cs",
            "runtimePlan.PipelineStrategy == FramePipelineStrategy.ReferenceQuality",
            "Pipeline: presetState.ActivePipeline",
            "PipelineId: presetState.ActivePreset.PipelineId",
            "Reason: \"Reference Quality uses the active preset pipeline instance.\"");
    }

    [Fact]
    public void Viewport_pipeline_selector_bypasses_pipeline_for_mesh_wireframe()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/runtime/STFU.UI/Viewport/ViewportFramePipelineSelector.cs",
            "contentKind != NprRenderContentKind.NprPipeline",
            "Pipeline: null",
            "Mesh wireframe rendering bypasses NPR frame pipeline strategy selection.");
    }

    [Fact]
    public void Viewport_pipeline_selector_caches_interactive_pipeline_instances()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/runtime/STFU.UI/Viewport/ViewportFramePipelineSelector.cs",
            "Dictionary<ViewportFramePipelineCacheKey, INprPipeline>",
            "_strategyPipelines.TryGetValue(key, out var pipeline)",
            "_strategyPipelines[key] = pipeline",
            "ViewportFramePipelineCacheKey");
    }

    [Fact]
    public void Viewport_interactive_options_default_to_safe_reference_output()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/runtime/STFU.UI/Viewport/ViewportInteractivePerformanceOptionsResolver.cs",
            "PreviewOutputVariable",
            "EnableInteractivePreviewOutput = previewOutput",
            "UseReferenceFallbackForFinalFrame = !previewOutput || forceFallback",
            "RequireToneCoverageForInteractivePreview = requireToneCoverage",
            "InteractivePreviewMinReadinessScore = minReadinessScore",
            "TargetFrameMs = targetFrameMs");
    }

    [Fact]
    public void Viewport_interactive_options_follow_direct_presentation_for_projection_preference()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/runtime/STFU.UI/Viewport/ViewportInteractivePerformanceOptionsResolver.cs",
            "PreferSelfContainedProjectionVariable",
            "runtimePlan.PreferGpuPresentation",
            "FramePipelineStrategyOptions.Default with");
    }

    [Fact]
    public void Viewport_request_factory_uses_pipeline_selection_for_request_identity()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/runtime/STFU.UI/Viewport/ViewportRenderRequestFactory.cs",
            "private readonly ViewportFramePipelineSelector _pipelineSelector = new();",
            "var pipelineSelection = _pipelineSelector.Select(contentKind, presetState, runtimePlan);",
            "Pipeline: pipelineSelection.Pipeline",
            "ActivePipelineId: pipelineSelection.PipelineId",
            "PipelineStrategy: pipelineSelection.Strategy");
    }

    [Fact]
    public void Runtime_plan_still_prefers_direct_presentation_for_interactive_performance()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/runtime/STFU.UI/Viewport/RendererRuntimePlan.cs",
            "pipelineStrategy == FramePipelineStrategy.InteractivePerformance && directPresenterAvailable",
            "requestedDirect = true",
            "PreferGpuPresentation: true",
            "AllowGpuReadback: false");
    }

    [Fact]
    public void Settings_window_keeps_interactive_performance_user_selection_contract()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/runtime/STFU.UI/Viewport/SettingsWindow.axaml.cs",
            "OnPipelineStrategySelectionChanged",
            "1 => FramePipelineStrategy.InteractivePerformance",
            "_renderer.PipelineStrategy = _draft.PipelineStrategy",
            "FramePipelineStrategyDisplay.GetDescription(_draft.PipelineStrategy)");
    }

    [Fact]
    public void Strategy_display_uses_stable_user_facing_names()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/runtime/STFU.UI/Viewport/FramePipelineStrategyDisplay.cs",
            "Reference Quality",
            "Interactive Performance",
            "Realtime-oriented pipeline",
            "Reference Quality until optimized stages are complete.");

        AssertFileDoesNotContain(
            repo,
            "src/runtime/STFU.UI/Viewport/FramePipelineStrategyDisplay.cs",
            "Legacy",
            "Old",
            "New",
            "Slow",
            "Fast");
    }

    [Fact]
    public void Interactive_signature_hash_includes_full_entity_transform()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractiveFrameHasher.cs",
            "using STFU.Common.Math;",
            "Mix(ulong hash, Transform3D value)",
            "value.Position",
            "value.Rotation",
            "value.Scale",
            "entity.Transform");
    }

    [Fact]
    public void Interactive_signature_hash_includes_entity_style_role()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractiveFrameSignatureFactory.cs",
            "context.EntityStyles.DefaultRole",
            "context.EntityStyles.GetRole(entity.Id)");
    }

    [Fact]
    public void Interactive_preview_policy_remains_opt_in()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractivePreviewPolicy.cs",
            "options.UseReferenceFallbackForFinalFrame",
            "EnableInteractivePreviewOutput is disabled.",
            "InteractivePreviewMinReadinessScore",
            "StrokeSegmentBudgetExceeded",
            "Interactive stroke frame selected for final viewport output.");
    }

    [Fact]
    public void Interactive_pipeline_executes_reference_first_before_harvesting_artifacts()
    {
        var repo = FindRepositoryRoot();
        var path = Path.Combine(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/InteractivePerformanceNprPipeline.cs");
        var text = File.ReadAllText(path);

        var fallback = text.IndexOf("referenceFrame = _referenceFallback.Execute(context);", StringComparison.Ordinal);
        var orchestrator = text.IndexOf("var result = _orchestrator.Execute(intent, context);", StringComparison.Ordinal);

        Assert.True(fallback >= 0, "Interactive Performance must execute Reference Quality while it harvests graph artifacts.");
        Assert.True(orchestrator > fallback, "Artifact harvesting must run after Reference Quality populates NprContext.Graph.");
    }

    [Fact]
    public void Interactive_output_selection_still_prefers_assembled_frame_artifacts()
    {
        var repo = FindRepositoryRoot();

        AssertFileContains(
            repo,
            "src/aot/npr/pipelines/STFU.NPR.Pipelines.InteractivePerformance/Core/InteractiveOutputSelector.cs",
            "InteractiveStrokeFrame",
            "VisibleStrokeSegments",
            "InteractivePreviewCandidate",
            "HasRenderableFrame");
    }

    private static void AssertFileContains(
        string repo,
        string relativePath,
        params string[] fragments)
    {
        var path = Path.Combine(repo, relativePath);
        Assert.True(File.Exists(path), $"Missing file: {path}");

        var text = File.ReadAllText(path);
        foreach (var fragment in fragments)
        {
            Assert.Contains(fragment, text);
        }
    }

    private static void AssertFileDoesNotContain(
        string repo,
        string relativePath,
        params string[] fragments)
    {
        var path = Path.Combine(repo, relativePath);
        Assert.True(File.Exists(path), $"Missing file: {path}");

        var text = File.ReadAllText(path);
        foreach (var fragment in fragments)
        {
            Assert.DoesNotContain(fragment, text);
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(directory))
        {
            if (File.Exists(Path.Combine(directory, "STFU.slnx")))
            {
                return directory;
            }

            var parent = Directory.GetParent(directory);
            if (parent is null)
            {
                break;
            }

            directory = parent.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate STFU.slnx from test output directory.");
    }
}
