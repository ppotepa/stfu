using STFU.Parallelism;
using Xunit;

namespace STFU.Parallelism.Tests;

public sealed class WorkerBudgetTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(32)]
    public void Resolve_SingleThreadDeterministic_ReturnsOne_ForAllLogicalCounts(int logicalProcessorCount)
    {
        var actual = WorkerBudget.ResolveForLogicalProcessorCount(
            new WorkerBudgetRequest(Mode: WorkerBudgetMode.SingleThreadDeterministic),
            logicalProcessorCount);

        Assert.Equal(1, actual);
    }

    [Fact]
    public void Resolve_ExplicitWorkerCount_IsClampedToLogicalProcessorCount()
    {
        var actual = WorkerBudget.ResolveForLogicalProcessorCount(
            new WorkerBudgetRequest(
                ExplicitWorkerCount: 20,
                MinimumWorkers: 1,
                MaximumWorkers: 0),
            logicalProcessorCount: 8);

        Assert.Equal(8, actual);
    }

    [Fact]
    public void Resolve_ExplicitWorkerCount_RespectsMinimumWorkers()
    {
        var actual = WorkerBudget.ResolveForLogicalProcessorCount(
            new WorkerBudgetRequest(
                ExplicitWorkerCount: 1,
                MinimumWorkers: 3,
                MaximumWorkers: 0),
            logicalProcessorCount: 16);

        Assert.Equal(3, actual);
    }

    [Fact]
    public void Resolve_ExplicitWorkerCount_RespectsMaximumWorkers()
    {
        var actual = WorkerBudget.ResolveForLogicalProcessorCount(
            new WorkerBudgetRequest(
                ExplicitWorkerCount: 20,
                MinimumWorkers: 1,
                MaximumWorkers: 8),
            logicalProcessorCount: 16);

        Assert.Equal(8, actual);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(4, 2)]
    [InlineData(8, 6)]
    [InlineData(16, 14)]
    [InlineData(32, 30)]
    public void Resolve_Balanced_ForLogicalCounts_1_2_4_8_16_32(
        int logicalProcessorCount,
        int expectedWorkerCount)
    {
        var actual = WorkerBudget.ResolveForLogicalProcessorCount(
            new WorkerBudgetRequest(Mode: WorkerBudgetMode.Balanced),
            logicalProcessorCount);

        Assert.Equal(expectedWorkerCount, actual);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(4, 3)]
    [InlineData(8, 7)]
    [InlineData(16, 15)]
    [InlineData(32, 31)]
    public void Resolve_Performance_ForLogicalCounts_1_2_4_8_16_32(
        int logicalProcessorCount,
        int expectedWorkerCount)
    {
        var actual = WorkerBudget.ResolveForLogicalProcessorCount(
            new WorkerBudgetRequest(Mode: WorkerBudgetMode.Performance),
            logicalProcessorCount);

        Assert.Equal(expectedWorkerCount, actual);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(4, 4)]
    [InlineData(8, 8)]
    [InlineData(16, 16)]
    [InlineData(32, 32)]
    public void Resolve_MaxPerformance_ForLogicalCounts_1_2_4_8_16_32(
        int logicalProcessorCount,
        int expectedWorkerCount)
    {
        var actual = WorkerBudget.ResolveForLogicalProcessorCount(
            new WorkerBudgetRequest(Mode: WorkerBudgetMode.MaxPerformance),
            logicalProcessorCount);

        Assert.Equal(expectedWorkerCount, actual);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(4, 4)]
    [InlineData(8, 8)]
    [InlineData(16, 16)]
    [InlineData(32, 32)]
    public void Resolve_Benchmark_ForLogicalCounts_1_2_4_8_16_32(
        int logicalProcessorCount,
        int expectedWorkerCount)
    {
        var actual = WorkerBudget.ResolveForLogicalProcessorCount(
            new WorkerBudgetRequest(Mode: WorkerBudgetMode.Benchmark),
            logicalProcessorCount);

        Assert.Equal(expectedWorkerCount, actual);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(4, 2)]
    [InlineData(8, 4)]
    [InlineData(16, 4)]
    [InlineData(32, 4)]
    public void Resolve_BackgroundSafe_ForLogicalCounts_1_2_4_8_16_32(
        int logicalProcessorCount,
        int expectedWorkerCount)
    {
        var actual = WorkerBudget.ResolveForLogicalProcessorCount(
            new WorkerBudgetRequest(Mode: WorkerBudgetMode.BackgroundSafe),
            logicalProcessorCount);

        Assert.Equal(expectedWorkerCount, actual);
    }

    [Fact]
    public void Resolve_MaximumBelowMinimum_DoesNotReturnBelowMinimumUnlessLogicalIsLower()
    {
        var actual = WorkerBudget.ResolveForLogicalProcessorCount(
            new WorkerBudgetRequest(
                Mode: WorkerBudgetMode.Balanced,
                MinimumWorkers: 8,
                MaximumWorkers: 4),
            logicalProcessorCount: 16);

        Assert.Equal(8, actual);
    }

    [Fact]
    public void Resolve_MaximumBelowMinimum_ClampsToLogicalProcessorCount_WhenLogicalIsLower()
    {
        var actual = WorkerBudget.ResolveForLogicalProcessorCount(
            new WorkerBudgetRequest(
                Mode: WorkerBudgetMode.Balanced,
                MinimumWorkers: 8,
                MaximumWorkers: 4),
            logicalProcessorCount: 4);

        Assert.Equal(4, actual);
    }
}
