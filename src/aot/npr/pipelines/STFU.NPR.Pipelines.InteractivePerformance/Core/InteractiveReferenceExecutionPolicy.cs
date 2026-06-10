using STFU.NPR.Pipelines.Abstractions;

namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed class InteractiveReferenceExecutionPolicy
{
    public const string EnvironmentVariable = "STFU_INTERACTIVE_REFERENCE_EXECUTION";

    public static InteractiveReferenceExecutionPolicy BeforeInteractive { get; } = new(
        InteractiveReferenceExecutionMode.BeforeInteractive,
        executeBeforeInteractive: true,
        allowLateFallback: false,
        "Reference Quality executes before Interactive Performance so the reference graph is available for artifact harvesting.");

    public InteractiveReferenceExecutionPolicy(
        InteractiveReferenceExecutionMode mode,
        bool executeBeforeInteractive,
        bool allowLateFallback,
        string reason)
    {
        Mode = mode;
        ExecuteBeforeInteractive = executeBeforeInteractive;
        AllowLateFallback = allowLateFallback;
        Reason = reason;
    }

    public InteractiveReferenceExecutionMode Mode { get; }

    public bool ExecuteBeforeInteractive { get; }

    public bool AllowLateFallback { get; }

    public string Reason { get; }

    public static InteractiveReferenceExecutionPolicy Resolve(FramePipelineStrategyOptions? options)
    {
        options ??= FramePipelineStrategyOptions.Default;

        var requested = Environment.GetEnvironmentVariable(EnvironmentVariable);
        if (string.IsNullOrWhiteSpace(requested))
        {
            return BeforeInteractive;
        }

        requested = requested.Trim();
        if (IsBeforeInteractive(requested))
        {
            return BeforeInteractive;
        }

        if (IsLateFallback(requested))
        {
            if (CanUseLateFallback(options))
            {
                return new InteractiveReferenceExecutionPolicy(
                    InteractiveReferenceExecutionMode.LateFallback,
                    executeBeforeInteractive: false,
                    allowLateFallback: true,
                    "Reference Quality is deferred until Interactive Performance explicitly needs fallback output.");
            }

            return new InteractiveReferenceExecutionPolicy(
                InteractiveReferenceExecutionMode.BeforeInteractive,
                executeBeforeInteractive: true,
                allowLateFallback: false,
                "Late fallback was requested but preview output is not enabled safely; Reference Quality remains the prepass.");
        }

        return BeforeInteractive;
    }

    private static bool CanUseLateFallback(FramePipelineStrategyOptions options)
    {
        return options.EnableInteractivePreviewOutput &&
               !options.UseReferenceFallbackForFinalFrame &&
               !options.ForceReferenceFallback;
    }

    private static bool IsBeforeInteractive(string value)
    {
        return value.Equals("before", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("before-interactive", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("prepass", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("reference-prepass", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("always", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsLateFallback(string value)
    {
        return value.Equals("late", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("late-fallback", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("defer", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("deferred", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("on-demand", StringComparison.OrdinalIgnoreCase);
    }
}
