using STFU.NPR.Pipeline.InteractivePerformance.Artifacts;
using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.NPR.Pipeline.InteractivePerformance.Stages;
using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;
using STFU.NPR.Pipelines.Abstractions;
using Xunit;

namespace STFU.NPR.Pipelines.Tests;

public sealed class InteractiveReferenceFreePreviewAndBudgetTests
{
    [Fact]
    public void Resolve_allows_reference_free_preview_only_when_preview_and_self_contained_projection_are_enabled()
    {
        var previous = Environment.GetEnvironmentVariable(InteractiveReferenceExecutionPolicy.EnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(InteractiveReferenceExecutionPolicy.EnvironmentVariable, "reference-free");
            var options = FramePipelineStrategyOptions.Default with
            {
                EnableInteractivePreviewOutput = true,
                EnableReferenceFreeInteractivePreview = true,
                UseReferenceFallbackForFinalFrame = false,
                PreferSelfContainedProjection = true,
                ForceReferenceFallback = false
            };

            var policy = InteractiveReferenceExecutionPolicy.Resolve(options);

            Assert.Equal(InteractiveReferenceExecutionMode.DisabledForViewportPreview, policy.Mode);
            Assert.False(policy.ExecuteBeforeInteractive);
            Assert.False(policy.AllowLateFallback);
            Assert.True(policy.ReferenceDisabledForPreview);
        }
        finally
        {
            Environment.SetEnvironmentVariable(InteractiveReferenceExecutionPolicy.EnvironmentVariable, previous);
        }
    }

    [Fact]
    public void Resolve_reverts_reference_free_preview_to_safe_prepass_when_gates_are_missing()
    {
        var previous = Environment.GetEnvironmentVariable(InteractiveReferenceExecutionPolicy.EnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(InteractiveReferenceExecutionPolicy.EnvironmentVariable, "reference-free");
            var options = FramePipelineStrategyOptions.Default with
            {
                EnableInteractivePreviewOutput = false,
                EnableReferenceFreeInteractivePreview = true,
                UseReferenceFallbackForFinalFrame = true,
                PreferSelfContainedProjection = true
            };

            var policy = InteractiveReferenceExecutionPolicy.Resolve(options);

            Assert.Equal(InteractiveReferenceExecutionMode.BeforeInteractive, policy.Mode);
            Assert.True(policy.ExecuteBeforeInteractive);
            Assert.False(policy.ReferenceDisabledForPreview);
        }
        finally
        {
            Environment.SetEnvironmentVariable(InteractiveReferenceExecutionPolicy.EnvironmentVariable, previous);
        }
    }

    [Fact]
    public void Budget_limiter_keeps_most_important_candidate_edges_deterministically()
    {
        var edges = new[]
        {
            CreateEdge(10, importance: 0.1f, length: 100),
            CreateEdge(20, importance: 2.0f, length: 8),
            CreateEdge(30, importance: 1.0f, length: 200),
            CreateEdge(40, importance: 2.0f, length: 12)
        };

        var limited = InteractiveBudgetLimiter.LimitCandidateEdges(edges, maxEdges: 2);

        Assert.Equal(2, limited.Length);
        Assert.Equal(40, limited[0].SourceEdgeId);
        Assert.Equal(20, limited[1].SourceEdgeId);
    }

    [Fact]
    public void Visible_segment_planner_honors_explicit_preview_budget()
    {
        var commands = Enumerable.Range(0, 16)
            .Select(index => new InteractiveStrokeCommand(
                SourceEdgeId: index,
                Role: 0,
                X0: 0,
                Y0: index,
                X1: 32,
                Y1: index + 1,
                Width: 1,
                Opacity: 1,
                Importance: 1,
                StyleKey: 0))
            .ToArray();

        var segments = VisibleStrokeSegmentPlanner.BuildSegments(
            commands,
            InteractiveQualityMode.QualityViewport,
            explicitMaxSegments: 3);

        Assert.Equal(3, segments.Length);
    }

    private static InteractiveCandidateEdge CreateEdge(long id, float importance, float length)
    {
        return new InteractiveCandidateEdge(
            SourceEdgeId: id,
            FaceA: 0,
            FaceB: 1,
            Role: 0,
            X0: 0,
            Y0: 0,
            X1: length,
            Y1: 0,
            ProjectedLength: length,
            Depth: 0.5f,
            Importance: importance);
    }
}
