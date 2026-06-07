using STFU.Rendering.Abstractions.Diagnostics;
using Xunit;

namespace STFU.Rendering.Abstractions.Tests;

public sealed class VisibilityParityStatsFallbackPolicyTests
{
    [Fact]
    public void Fallback_CarriesReasonAndForcesFallback()
    {
        var stats = VisibilityParityStats.Fallback("gpu readback failed", cpuVisibleFaces: 12, gpuVisibleFaces: 0);

        Assert.True(stats.FallbackUsed);
        Assert.True(stats.ShouldFallback());
        Assert.False(stats.Passed);
        Assert.Equal("gpu readback failed", stats.FallbackReason);
        Assert.Contains("fallback=True", stats.ToDiagnosticString());
    }

    [Fact]
    public void FromCounts_UsesCpuOnlyAndGpuOnlyAsMismatchAliases()
    {
        var stats = VisibilityParityStats.FromCounts(
            cpuVisibleFaces: 100,
            gpuVisibleFaces: 102,
            matchingFaces: 98,
            cpuOnlyFaces: 2,
            gpuOnlyFaces: 4,
            requiredMatchRatio: 0.995f);

        Assert.Equal(2, stats.MissingOnGpu);
        Assert.Equal(4, stats.ExtraOnGpu);
        Assert.Equal(6, stats.MismatchCount);
        Assert.True(stats.ShouldFallback());
    }
}
