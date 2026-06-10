using STFU.NPR.Pipelines.Abstractions;

namespace STFU.UI;

internal static class ViewportInteractivePerformanceOptionsResolver
{
    internal const string PreviewOutputVariable = "STFU_INTERACTIVE_PREVIEW_OUTPUT";
    internal const string ForceReferenceFallbackVariable = "STFU_INTERACTIVE_FORCE_REFERENCE_FALLBACK";
    internal const string RequireToneCoverageVariable = "STFU_INTERACTIVE_PREVIEW_REQUIRE_TONE";
    internal const string MaxStrokeSegmentsVariable = "STFU_INTERACTIVE_PREVIEW_MAX_SEGMENTS";
    internal const string PreferSelfContainedProjectionVariable = "STFU_INTERACTIVE_PREFER_SELF_CONTAINED_PROJECTION";
    internal const string MaxFrameArtifactsPerKindVariable = "STFU_INTERACTIVE_MAX_FRAME_ARTIFACTS_PER_KIND";
    internal const string MaxFrameArtifactsTotalVariable = "STFU_INTERACTIVE_MAX_FRAME_ARTIFACTS_TOTAL";

    private const int MinimumPreviewStrokeSegments = 128;
    private const int MaximumPreviewStrokeSegments = 262_144;
    private const int MinimumFrameArtifactsPerKind = 1;
    private const int MaximumFrameArtifactsPerKind = 32;
    private const int MinimumTotalFrameArtifacts = 8;
    private const int MaximumTotalFrameArtifacts = 4096;

    public static FramePipelineStrategyOptions Create(RendererRuntimePlan runtimePlan)
    {
        var previewOutput = ReadBool(PreviewOutputVariable) ?? false;
        var forceFallback = ReadBool(ForceReferenceFallbackVariable) ?? false;
        var requireToneCoverage = ReadBool(RequireToneCoverageVariable) ?? false;
        var maxStrokeSegments = ReadBoundedInt(
            MaxStrokeSegmentsVariable,
            defaultValue: 0,
            min: MinimumPreviewStrokeSegments,
            max: MaximumPreviewStrokeSegments);
        var preferSelfContainedProjection = ReadBool(PreferSelfContainedProjectionVariable) ??
            runtimePlan.PreferGpuPresentation;
        var maxFrameArtifactsPerKind = ReadBoundedInt(
            MaxFrameArtifactsPerKindVariable,
            FramePipelineStrategyOptions.Default.MaxFrameOrCameraArtifactsPerKind,
            MinimumFrameArtifactsPerKind,
            MaximumFrameArtifactsPerKind);
        var maxTotalFrameArtifacts = ReadBoundedInt(
            MaxFrameArtifactsTotalVariable,
            FramePipelineStrategyOptions.Default.MaxTotalFrameOrCameraArtifacts,
            MinimumTotalFrameArtifacts,
            MaximumTotalFrameArtifacts);

        return FramePipelineStrategyOptions.Default with
        {
            ForceReferenceFallback = forceFallback,
            PreferSelfContainedProjection = preferSelfContainedProjection,
            EnableInteractivePreviewOutput = previewOutput,
            UseReferenceFallbackForFinalFrame = !previewOutput || forceFallback,
            RequireToneCoverageForInteractivePreview = requireToneCoverage,
            InteractivePreviewMaxStrokeSegments = maxStrokeSegments,
            MaxFrameOrCameraArtifactsPerKind = maxFrameArtifactsPerKind,
            MaxTotalFrameOrCameraArtifacts = maxTotalFrameArtifacts,
            TargetFrameMs = 16.6
        };
    }

    private static bool? ReadBool(string variable)
    {
        var value = Environment.GetEnvironmentVariable(variable);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        value = value.Trim();
        if (value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("on", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (value.Equals("0", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("false", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("no", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("off", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return null;
    }

    private static int ReadBoundedInt(
        string variable,
        int defaultValue,
        int min,
        int max)
    {
        var value = Environment.GetEnvironmentVariable(variable);
        if (!int.TryParse(value, out var parsed))
        {
            return defaultValue;
        }

        if (parsed <= 0)
        {
            return defaultValue;
        }

        return Math.Clamp(parsed, min, max);
    }
}
