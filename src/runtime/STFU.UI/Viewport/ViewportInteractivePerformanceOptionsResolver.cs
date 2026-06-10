using STFU.NPR.Pipelines.Abstractions;

namespace STFU.UI;

internal static class ViewportInteractivePerformanceOptionsResolver
{
    internal const string PreviewOutputVariable = "STFU_INTERACTIVE_PREVIEW_OUTPUT";
    internal const string ForceReferenceFallbackVariable = "STFU_INTERACTIVE_FORCE_REFERENCE_FALLBACK";
    internal const string RequireToneCoverageVariable = "STFU_INTERACTIVE_PREVIEW_REQUIRE_TONE";
    internal const string MaxStrokeSegmentsVariable = "STFU_INTERACTIVE_PREVIEW_MAX_SEGMENTS";
    internal const string MinReadinessScoreVariable = "STFU_INTERACTIVE_PREVIEW_MIN_READINESS_SCORE";
    internal const string PreferSelfContainedProjectionVariable = "STFU_INTERACTIVE_PREFER_SELF_CONTAINED_PROJECTION";
    internal const string ReferenceFreePreviewVariable = "STFU_INTERACTIVE_REFERENCE_FREE_PREVIEW";
    internal const string DeferToneCoverageVariable = "STFU_INTERACTIVE_DEFER_TONE_WHEN_OPTIONAL";
    internal const string MaxCandidateEdgesVariable = "STFU_INTERACTIVE_MAX_CANDIDATE_EDGES";
    internal const string MaxStrokeCommandsVariable = "STFU_INTERACTIVE_MAX_STROKE_COMMANDS";
    internal const string MaxVisibleSegmentsVariable = "STFU_INTERACTIVE_MAX_VISIBLE_SEGMENTS";
    internal const string TargetFrameMsVariable = "STFU_INTERACTIVE_TARGET_FRAME_MS";
    internal const string MaxFrameArtifactsPerKindVariable = "STFU_INTERACTIVE_MAX_FRAME_ARTIFACTS_PER_KIND";
    internal const string MaxFrameArtifactsTotalVariable = "STFU_INTERACTIVE_MAX_FRAME_ARTIFACTS_TOTAL";

    private const int MinimumPreviewStrokeSegments = 128;
    private const int MaximumPreviewStrokeSegments = 262_144;
    private const int MinimumPreviewReadinessScore = 0;
    private const int MaximumPreviewReadinessScore = 100;
    private const int MinimumCandidateEdges = 256;
    private const int MaximumCandidateEdges = 1_000_000;
    private const int MinimumStrokeCommands = 128;
    private const int MaximumStrokeCommands = 1_000_000;
    private const int MinimumVisibleSegments = 128;
    private const int MaximumVisibleSegments = 262_144;
    private const int MinimumFrameArtifactsPerKind = 1;
    private const int MaximumFrameArtifactsPerKind = 32;
    private const int MinimumTotalFrameArtifacts = 8;
    private const int MaximumTotalFrameArtifacts = 4096;
    private const double MinimumTargetFrameMs = 4d;
    private const double MaximumTargetFrameMs = 100d;

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
        var minReadinessScore = ReadBoundedInt(
            MinReadinessScoreVariable,
            defaultValue: FramePipelineStrategyOptions.Default.InteractivePreviewMinReadinessScore,
            min: MinimumPreviewReadinessScore,
            max: MaximumPreviewReadinessScore);
        var preferSelfContainedProjection = ReadBool(PreferSelfContainedProjectionVariable) ??
            runtimePlan.PreferGpuPresentation;
        var referenceFreePreview = ReadBool(ReferenceFreePreviewVariable) ?? false;
        var deferToneCoverage = ReadBool(DeferToneCoverageVariable) ?? false;
        var maxCandidateEdges = ReadBoundedInt(
            MaxCandidateEdgesVariable,
            FramePipelineStrategyOptions.Default.MaxInteractiveCandidateEdges,
            MinimumCandidateEdges,
            MaximumCandidateEdges);
        var maxStrokeCommands = ReadBoundedInt(
            MaxStrokeCommandsVariable,
            FramePipelineStrategyOptions.Default.MaxInteractiveStrokeCommands,
            MinimumStrokeCommands,
            MaximumStrokeCommands);
        var maxVisibleSegments = ReadBoundedInt(
            MaxVisibleSegmentsVariable,
            FramePipelineStrategyOptions.Default.MaxInteractiveVisibleStrokeSegments,
            MinimumVisibleSegments,
            MaximumVisibleSegments);
        var targetFrameMs = ReadBoundedDouble(
            TargetFrameMsVariable,
            FramePipelineStrategyOptions.Default.TargetFrameMs,
            MinimumTargetFrameMs,
            MaximumTargetFrameMs);
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
            EnableReferenceFreeInteractivePreview = referenceFreePreview && previewOutput && preferSelfContainedProjection && !forceFallback,
            EnableInteractivePreviewOutput = previewOutput,
            UseReferenceFallbackForFinalFrame = !previewOutput || forceFallback,
            RequireToneCoverageForInteractivePreview = requireToneCoverage,
            InteractivePreviewMaxStrokeSegments = maxStrokeSegments,
            InteractivePreviewMinReadinessScore = minReadinessScore,
            MaxInteractiveCandidateEdges = maxCandidateEdges,
            MaxInteractiveStrokeCommands = maxStrokeCommands,
            MaxInteractiveVisibleStrokeSegments = maxVisibleSegments,
            DeferToneCoverageWhenPreviewDoesNotRequireTone = deferToneCoverage,
            MaxFrameOrCameraArtifactsPerKind = maxFrameArtifactsPerKind,
            MaxTotalFrameOrCameraArtifacts = maxTotalFrameArtifacts,
            TargetFrameMs = targetFrameMs
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

    private static double ReadBoundedDouble(
        string variable,
        double defaultValue,
        double min,
        double max)
    {
        var value = Environment.GetEnvironmentVariable(variable);
        if (!double.TryParse(value, out var parsed))
        {
            return defaultValue;
        }

        if (parsed <= 0d)
        {
            return defaultValue;
        }

        return Math.Clamp(parsed, min, max);
    }
}
