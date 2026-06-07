using STFU.Rendering.Abstractions.Diagnostics;
using Xunit;

namespace STFU.Rendering.Abstractions.Tests;

public sealed class VisibilityParityFallbackDecisionTests
{
    [Fact]
    public void ExactMatch_DoesNotFallback()
    {
        var stats = VisibilityParityStats.FromCounts(
            cpuVisibleFaces: 100,
            gpuVisibleFaces: 100,
            matchingFaces: 100,
            cpuOnlyFaces: 0,
            gpuOnlyFaces: 0);

        Assert.True(stats.Passed);
        Assert.False(stats.ShouldFallback());
        Assert.False(stats.FallbackUsed);
    }

    [Fact]
    public void SmallMismatchAboveThreshold_DoesNotFallback()
    {
        var stats = VisibilityParityStats.FromCounts(
            cpuVisibleFaces: 1000,
            gpuVisibleFaces: 999,
            matchingFaces: 999,
            cpuOnlyFaces: 1,
            gpuOnlyFaces: 0,
            requiredMatchRatio: 0.995f);

        Assert.True(stats.Passed);
        Assert.False(stats.ShouldFallback(0.995f));
    }

    [Fact]
    public void LargeMismatchBelowThreshold_Fallbacks()
    {
        var stats = VisibilityParityStats.FromCounts(
            cpuVisibleFaces: 1000,
            gpuVisibleFaces: 920,
            matchingFaces: 920,
            cpuOnlyFaces: 80,
            gpuOnlyFaces: 0,
            requiredMatchRatio: 0.995f);

        Assert.False(stats.Passed);
        Assert.True(stats.ShouldFallback(0.995f));
    }

    [Fact]
    public void ExplicitFallback_AlwaysFallbacksAndRecordsReason()
    {
        var stats = VisibilityParityStats.Fallback("GpuVisibilityReadbackFailed", 123, 0);

        Assert.True(stats.FallbackUsed);
        Assert.Equal("GpuVisibilityReadbackFailed", stats.FallbackReason);
        Assert.True(stats.ShouldFallback());
        Assert.Contains("fallback=True", stats.ToDiagnosticString(), StringComparison.OrdinalIgnoreCase);
    }
}
