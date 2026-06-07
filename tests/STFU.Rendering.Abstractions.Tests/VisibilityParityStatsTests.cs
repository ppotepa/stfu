using STFU.Rendering.Abstractions.Diagnostics;
using Xunit;

namespace STFU.Rendering.Abstractions.Tests;

public sealed class VisibilityParityStatsTests
{
    [Fact]
    public void FromCounts_AllMatching_PassesWithoutFallback()
    {
        var stats = VisibilityParityStats.FromCounts(
            cpuVisibleFaces: 128,
            gpuVisibleFaces: 128,
            matchingFaces: 128,
            cpuOnlyFaces: 0,
            gpuOnlyFaces: 0);

        Assert.True(stats.Passed);
        Assert.False(stats.ShouldFallback());
        Assert.Equal(1f, stats.MatchRatio);
    }

    [Fact]
    public void FromCounts_MismatchBelowThreshold_RequestsFallback()
    {
        var stats = VisibilityParityStats.FromCounts(
            cpuVisibleFaces: 100,
            gpuVisibleFaces: 96,
            matchingFaces: 96,
            cpuOnlyFaces: 4,
            gpuOnlyFaces: 0);

        Assert.False(stats.Passed);
        Assert.True(stats.ShouldFallback());
        Assert.Equal(4, stats.MismatchCount);
    }
}
