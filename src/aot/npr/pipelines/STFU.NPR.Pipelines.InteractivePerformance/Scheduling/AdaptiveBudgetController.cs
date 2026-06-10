using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.NPR.Pipelines.Abstractions;

namespace STFU.NPR.Pipeline.InteractivePerformance.Scheduling;

public sealed class AdaptiveBudgetController
{
    private const double OverBudgetFactor = 1.18d;
    private const double UnderBudgetFactor = 0.62d;
    private const int DowngradeStreakThreshold = 2;
    private const int UpgradeStreakThreshold = 4;

    private int _overBudgetStreak;
    private int _underBudgetStreak;

    public InteractiveBudgetDecision ResolveBudgetDecision(
        InteractiveFrameIntent intent,
        InteractiveFrameDiagnostics previous,
        FramePipelineStrategyOptions options)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(options);

        var requested = intent.QualityMode == InteractiveQualityMode.Auto
            ? InteractiveQualityMode.BalancedViewport
            : intent.QualityMode;
        var previousKnownMs = KnownFrameMs(previous);
        var targetFrameMs = ResolveTargetFrameMs(intent, options);
        var pressure = ResolvePressure(previousKnownMs, targetFrameMs);
        UpdateStreaks(pressure);

        var resolved = ResolveQualityMode(
            requested,
            pressure,
            _overBudgetStreak,
            _underBudgetStreak);

        return new InteractiveBudgetDecision(
            RequestedQualityMode: requested,
            ResolvedQualityMode: resolved,
            Pressure: pressure,
            PreviousKnownFrameMs: previousKnownMs,
            TargetFrameMs: targetFrameMs,
            OverBudgetStreak: _overBudgetStreak,
            UnderBudgetStreak: _underBudgetStreak,
            QualityChanged: resolved != requested,
            EffectiveMaxCandidateEdges: ScaleBudget(options.MaxInteractiveCandidateEdges, resolved, pressure),
            EffectiveMaxStrokeCommands: ScaleBudget(options.MaxInteractiveStrokeCommands, resolved, pressure),
            EffectiveMaxVisibleStrokeSegments: ScaleBudget(options.MaxInteractiveVisibleStrokeSegments, resolved, pressure),
            EffectiveToneDeferred: ResolveToneDeferral(options, resolved, pressure));
    }

    public InteractiveQualityMode ResolveQualityMode(
        InteractiveFrameIntent intent,
        InteractiveFrameDiagnostics previous)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(previous);

        return ResolveBudgetDecision(intent, previous, intent.Options).ResolvedQualityMode;
    }

    public InteractiveQualityMode SelectNextQuality(
        InteractiveQualityMode current,
        InteractiveFrameDiagnostics previous,
        TimeSpan targetFrameTime)
    {
        ArgumentNullException.ThrowIfNull(previous);

        var targetFrameMs = targetFrameTime.TotalMilliseconds <= 0d
            ? 16.6d
            : targetFrameTime.TotalMilliseconds;
        var pressure = ResolvePressure(KnownFrameMs(previous), targetFrameMs);
        return ResolveQualityMode(current, pressure, DowngradeStreakThreshold, UpgradeStreakThreshold);
    }

    public static double KnownFrameMs(InteractiveFrameDiagnostics previous)
    {
        ArgumentNullException.ThrowIfNull(previous);

        return previous.TotalInteractiveStageMs;
    }

    private static double ResolveTargetFrameMs(
        InteractiveFrameIntent intent,
        FramePipelineStrategyOptions options)
    {
        if (options.TargetFrameMs > 0d)
        {
            return options.TargetFrameMs;
        }

        return intent.FrameBudget.TotalMilliseconds > 0d
            ? intent.FrameBudget.TotalMilliseconds
            : 16.6d;
    }

    private static InteractiveBudgetPressure ResolvePressure(double knownFrameMs, double targetFrameMs)
    {
        if (knownFrameMs <= 0d || targetFrameMs <= 0d)
        {
            return InteractiveBudgetPressure.Unknown;
        }

        if (knownFrameMs > targetFrameMs * OverBudgetFactor)
        {
            return InteractiveBudgetPressure.OverBudget;
        }

        if (knownFrameMs < targetFrameMs * UnderBudgetFactor)
        {
            return InteractiveBudgetPressure.UnderBudget;
        }

        return InteractiveBudgetPressure.Stable;
    }

    private void UpdateStreaks(InteractiveBudgetPressure pressure)
    {
        switch (pressure)
        {
            case InteractiveBudgetPressure.OverBudget:
                _overBudgetStreak++;
                _underBudgetStreak = 0;
                break;
            case InteractiveBudgetPressure.UnderBudget:
                _underBudgetStreak++;
                _overBudgetStreak = 0;
                break;
            case InteractiveBudgetPressure.Stable:
                _overBudgetStreak = Math.Max(0, _overBudgetStreak - 1);
                _underBudgetStreak = Math.Max(0, _underBudgetStreak - 1);
                break;
            default:
                _overBudgetStreak = 0;
                _underBudgetStreak = 0;
                break;
        }
    }

    private static InteractiveQualityMode ResolveQualityMode(
        InteractiveQualityMode current,
        InteractiveBudgetPressure pressure,
        int overBudgetStreak,
        int underBudgetStreak)
    {
        if (pressure == InteractiveBudgetPressure.OverBudget && overBudgetStreak >= DowngradeStreakThreshold)
        {
            return current switch
            {
                InteractiveQualityMode.QualityViewport => InteractiveQualityMode.BalancedViewport,
                InteractiveQualityMode.BalancedViewport => InteractiveQualityMode.FastPreview,
                _ => InteractiveQualityMode.FastPreview
            };
        }

        if (pressure == InteractiveBudgetPressure.UnderBudget && underBudgetStreak >= UpgradeStreakThreshold)
        {
            return current switch
            {
                InteractiveQualityMode.FastPreview => InteractiveQualityMode.BalancedViewport,
                InteractiveQualityMode.BalancedViewport => InteractiveQualityMode.QualityViewport,
                _ => current
            };
        }

        return current;
    }

    private static int ScaleBudget(
        int configured,
        InteractiveQualityMode qualityMode,
        InteractiveBudgetPressure pressure)
    {
        if (configured <= 0)
        {
            return configured;
        }

        var factor = qualityMode switch
        {
            InteractiveQualityMode.FastPreview => 0.35d,
            InteractiveQualityMode.BalancedViewport => 0.70d,
            InteractiveQualityMode.QualityViewport => 1.0d,
            _ => 0.70d
        };

        if (pressure == InteractiveBudgetPressure.OverBudget)
        {
            factor *= 0.75d;
        }
        else if (pressure == InteractiveBudgetPressure.UnderBudget)
        {
            factor = Math.Min(1.0d, factor * 1.15d);
        }

        return Math.Max(1, (int)Math.Ceiling(configured * factor));
    }

    private static bool ResolveToneDeferral(
        FramePipelineStrategyOptions options,
        InteractiveQualityMode qualityMode,
        InteractiveBudgetPressure pressure)
    {
        if (options.RequireToneCoverageForInteractivePreview)
        {
            return false;
        }

        return options.DeferToneCoverageWhenPreviewDoesNotRequireTone ||
            qualityMode == InteractiveQualityMode.FastPreview ||
            pressure == InteractiveBudgetPressure.OverBudget;
    }
}
