using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.NPR.Pipelines.Abstractions;
using Xunit;

namespace STFU.NPR.Pipelines.Tests;

public sealed class InteractiveFrameOrchestratorTests
{
    [Fact]
    public void Diagnostics_records_stage_timing()
    {
        var diagnostics = new InteractiveFrameDiagnostics();

        diagnostics.AddStageTiming("InteractiveProjection", TimeSpan.FromMilliseconds(1.25));

        Assert.Equal(1.25, diagnostics.ProjectionMs);
        Assert.True(diagnostics.StageTimingsMs.ContainsKey("InteractiveProjection"));
        Assert.Equal(1.25, diagnostics.StageTimingsMs["InteractiveProjection"]);
    }

    [Fact]
    public void Pipeline_result_requires_reference_fallback_when_diagnostics_says_so()
    {
        var diagnostics = new InteractiveFrameDiagnostics
        {
            Strategy = FramePipelineStrategy.InteractivePerformance,
            UsedReferenceFallback = true
        };

        var result = new InteractivePipelineResult(diagnostics);

        Assert.True(result.RequiresReferenceFallback);
        Assert.Equal("InteractivePerformance", diagnostics.Strategy.ToString());
    }
}
