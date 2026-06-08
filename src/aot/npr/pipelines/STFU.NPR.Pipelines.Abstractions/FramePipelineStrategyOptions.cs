namespace STFU.NPR.Pipelines.Abstractions;

public sealed record FramePipelineStrategyOptions
{
    public static FramePipelineStrategyOptions Default { get; } = new();

    public bool EnableDiagnostics { get; init; }
}
