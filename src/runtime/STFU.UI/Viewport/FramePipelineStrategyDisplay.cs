using STFU.NPR.Pipelines.Abstractions;

namespace STFU.UI.Viewport;

public static class FramePipelineStrategyDisplay
{
    public static string GetDisplayName(FramePipelineStrategy strategy) => strategy switch
    {
        FramePipelineStrategy.ReferenceQuality => "Reference Quality",
        FramePipelineStrategy.InteractivePerformance => "Interactive Performance",
        _ => strategy.ToString()
    };

    public static string GetDescription(FramePipelineStrategy strategy) => strategy switch
    {
        FramePipelineStrategy.ReferenceQuality => "Full reference NPR pipeline used for validation, export, parity, and highest-quality rendering.",
        FramePipelineStrategy.InteractivePerformance => "Realtime-oriented pipeline. Uses cache-aware artifacts, budgeted updates and direct GPU presentation. Early versions may fall back to Reference Quality for incomplete stages.",
        _ => string.Empty
    };

    public static string GetStatusNote(FramePipelineStrategy strategy) => strategy switch
    {
        FramePipelineStrategy.InteractivePerformance => "Interactive Performance currently delegates final output to Reference Quality until optimized stages are complete.",
        _ => string.Empty
    };
}
