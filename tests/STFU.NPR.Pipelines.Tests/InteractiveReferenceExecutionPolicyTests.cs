using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.NPR.Pipelines.Abstractions;
using Xunit;

namespace STFU.NPR.Pipelines.Tests;

[Collection(InteractiveReferenceExecutionEnvironmentCollection.Name)]
public sealed class InteractiveReferenceExecutionPolicyTests
{
    [Fact]
    public void Resolve_defaults_to_reference_prepass()
    {
        WithEnvironment(null, () =>
        {
            var policy = InteractiveReferenceExecutionPolicy.Resolve(FramePipelineStrategyOptions.Default);

            Assert.Equal(InteractiveReferenceExecutionMode.BeforeInteractive, policy.Mode);
            Assert.True(policy.ExecuteBeforeInteractive);
            Assert.False(policy.AllowLateFallback);
        });
    }

    [Fact]
    public void Resolve_allows_late_fallback_only_for_explicit_preview_output()
    {
        WithEnvironment("late-fallback", () =>
        {
            var options = FramePipelineStrategyOptions.Default with
            {
                EnableInteractivePreviewOutput = true,
                UseReferenceFallbackForFinalFrame = false,
                ForceReferenceFallback = false
            };

            var policy = InteractiveReferenceExecutionPolicy.Resolve(options);

            Assert.Equal(InteractiveReferenceExecutionMode.LateFallback, policy.Mode);
            Assert.False(policy.ExecuteBeforeInteractive);
            Assert.True(policy.AllowLateFallback);
        });
    }

    [Fact]
    public void Resolve_rejects_late_fallback_when_reference_output_is_required()
    {
        WithEnvironment("late-fallback", () =>
        {
            var options = FramePipelineStrategyOptions.Default with
            {
                EnableInteractivePreviewOutput = false,
                UseReferenceFallbackForFinalFrame = true
            };

            var policy = InteractiveReferenceExecutionPolicy.Resolve(options);

            Assert.Equal(InteractiveReferenceExecutionMode.BeforeInteractive, policy.Mode);
            Assert.True(policy.ExecuteBeforeInteractive);
            Assert.False(policy.AllowLateFallback);
        });
    }

    private static void WithEnvironment(string? value, Action action)
    {
        var previous = Environment.GetEnvironmentVariable(InteractiveReferenceExecutionPolicy.EnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(InteractiveReferenceExecutionPolicy.EnvironmentVariable, value);
            action();
        }
        finally
        {
            Environment.SetEnvironmentVariable(InteractiveReferenceExecutionPolicy.EnvironmentVariable, previous);
        }
    }
}
