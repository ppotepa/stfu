using STFU.NPR.Pipeline.InteractivePerformance.Core;
using Xunit;

namespace STFU.NPR.Pipelines.Tests;

public sealed class InteractiveDiagnosticsTests
{
    [Fact]
    public void AddStageTiming_records_projection_ms()
    {
        var diagnostics = new InteractiveFrameDiagnostics();

        diagnostics.AddStageTiming("InteractiveProjection", TimeSpan.FromMilliseconds(2.5));

        Assert.Equal(2.5, diagnostics.ProjectionMs, precision: 3);
        Assert.True(diagnostics.StageTimingsMs.ContainsKey("InteractiveProjection"));
    }

    [Fact]
    public void PipelineResult_requires_fallback_when_diagnostics_says_so()
    {
        var diagnostics = new InteractiveFrameDiagnostics
        {
            UsedReferenceFallback = true
        };

        var result = new InteractivePipelineResult(diagnostics);

        Assert.True(result.RequiresReferenceFallback);
    }
}
