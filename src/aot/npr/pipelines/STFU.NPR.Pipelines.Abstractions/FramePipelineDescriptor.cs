namespace STFU.NPR.Pipelines.Abstractions;

public sealed record FramePipelineDescriptor(
    FramePipelineStrategy Strategy,
    string PipelineId,
    string DisplayName,
    string Description);
