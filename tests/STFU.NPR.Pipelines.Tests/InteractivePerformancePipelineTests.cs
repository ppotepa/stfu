using STFU.NPR.Pipeline.InteractivePerformance;
using STFU.NPR.Pipelines.Abstractions;
using STFU.NPR.Pipeline;
using STFU.Strokes;
using Xunit;

namespace STFU.NPR.Pipelines.Tests;

public sealed class InteractivePerformancePipelineTests
{
    [Fact]
    public void Create_returns_interactive_pipeline_instance()
    {
        var pipeline = InteractivePerformancePipeline.Create();

        Assert.NotNull(pipeline);
        Assert.IsType<InteractivePerformanceNprPipeline>(pipeline);
        Assert.IsAssignableFrom<INprPipeline>(pipeline);
    }

    [Fact]
    public void Create_with_options_returns_interactive_pipeline_instance()
    {
        var pipeline = InteractivePerformancePipeline.Create(FramePipelineStrategyOptions.Default);

        Assert.NotNull(pipeline);
        Assert.IsType<InteractivePerformanceNprPipeline>(pipeline);
        Assert.IsAssignableFrom<INprPipeline>(pipeline);
    }
}
