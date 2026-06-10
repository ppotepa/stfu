using STFU.NPR.Pipeline.InteractivePerformance.Core;
using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;
using STFU.NPR.Pipeline.InteractivePerformance.Stages;
using STFU.NPR.Pipelines.Abstractions;
using Xunit;

namespace STFU.NPR.Pipelines.Tests;

public sealed class InteractiveAdaptiveBudgetDiagnosticsTests
{
    [Fact]
    public void Diagnostics_capture_adaptive_budget_decision()
    {
        var diagnostics = new InteractiveFrameDiagnostics();
        var decision = new InteractiveBudgetDecision(
            RequestedQualityMode: InteractiveQualityMode.BalancedViewport,
            ResolvedQualityMode: InteractiveQualityMode.FastPreview,
            Pressure: InteractiveBudgetPressure.OverBudget,
            PreviousKnownFrameMs: 32.5,
            TargetFrameMs: 16.6,
            OverBudgetStreak: 2,
            UnderBudgetStreak: 0,
            QualityChanged: true,
            EffectiveMaxCandidateEdges: 256,
            EffectiveMaxStrokeCommands: 128,
            EffectiveMaxVisibleStrokeSegments: 64,
            EffectiveToneDeferred: true);

        diagnostics.CaptureBudgetDecision(decision);

        Assert.Equal(InteractiveQualityMode.BalancedViewport, diagnostics.RequestedQualityMode);
        Assert.Equal(InteractiveQualityMode.FastPreview, diagnostics.ResolvedQualityMode);
        Assert.Equal((long)InteractiveBudgetPressure.OverBudget, diagnostics.BudgetPressure);
        Assert.Equal(32.5, diagnostics.PreviousKnownFrameMs);
        Assert.True(diagnostics.BudgetQualityChanged);
        Assert.Equal(256, diagnostics.EffectiveMaxCandidateEdges);
        Assert.True(diagnostics.EffectiveToneDeferred);
    }

    [Fact]
    public void Budget_decision_applies_effective_frame_options()
    {
        var options = FramePipelineStrategyOptions.Default with
        {
            MaxInteractiveCandidateEdges = 10_000,
            MaxInteractiveStrokeCommands = 8_000,
            MaxInteractiveVisibleStrokeSegments = 6_000,
            DeferToneCoverageWhenPreviewDoesNotRequireTone = false
        };
        var decision = new InteractiveBudgetDecision(
            InteractiveQualityMode.BalancedViewport,
            InteractiveQualityMode.FastPreview,
            InteractiveBudgetPressure.OverBudget,
            30,
            16.6,
            2,
            0,
            true,
            100,
            80,
            60,
            true);

        var applied = decision.ApplyTo(options);

        Assert.Equal(100, applied.MaxInteractiveCandidateEdges);
        Assert.Equal(80, applied.MaxInteractiveStrokeCommands);
        Assert.Equal(60, applied.MaxInteractiveVisibleStrokeSegments);
        Assert.True(applied.DeferToneCoverageWhenPreviewDoesNotRequireTone);
    }
}
