using STFU.Rendering.Abstractions.Diagnostics;
using Xunit;

namespace STFU.Rendering.Abstractions.Tests;

public sealed class VisibilityParityStatsFinalContractTests
{
    [Fact]
    public void FromCounts_PerfectMatch_DoesNotRequestFallback()
    {
        var stats = VisibilityParityStats.FromCounts(
            cpuVisibleFaces: 256,
            gpuVisibleFaces: 256,
            matchingFaces: 256,
            cpuOnlyFaces: 0,
            gpuOnlyFaces: 0);

        Assert.True(stats.Passed);
        Assert.False(stats.FallbackUsed);
        Assert.False(stats.ShouldFallback());
        Assert.Equal(0, stats.MismatchCount);
    }

    [Theory]
    [InlineData(8, 0)]
    [InlineData(0, 8)]
    [InlineData(4, 4)]
    public void FromCounts_MismatchBelowThreshold_RequestsFallback(int cpuOnlyFaces, int gpuOnlyFaces)
    {
        var stats = VisibilityParityStats.FromCounts(
            cpuVisibleFaces: 128,
            gpuVisibleFaces: 128 - cpuOnlyFaces + gpuOnlyFaces,
            matchingFaces: 128 - cpuOnlyFaces,
            cpuOnlyFaces: cpuOnlyFaces,
            gpuOnlyFaces: gpuOnlyFaces);

        Assert.False(stats.Passed);
        Assert.True(stats.ShouldFallback());
        Assert.Equal(cpuOnlyFaces, stats.MissingOnGpu);
        Assert.Equal(gpuOnlyFaces, stats.ExtraOnGpu);
    }

    [Fact]
    public void Fallback_PreservesReasonAndFailsClosed()
    {
        var stats = VisibilityParityStats.Fallback("gpu visibility readback failed", cpuVisibleFaces: 12, gpuVisibleFaces: 0);

        Assert.True(stats.FallbackUsed);
        Assert.True(stats.ShouldFallback());
        Assert.False(stats.Passed);
        Assert.Equal("gpu visibility readback failed", stats.FallbackReason);
        Assert.Contains("fallback=True", stats.ToDiagnosticString());
    }
}
