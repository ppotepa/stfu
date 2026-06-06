using STFU.Parallelism;
using STFU.Rendering.Abstractions.Requests;
using Xunit;

namespace STFU.Rendering.Abstractions.Tests;

public sealed class NprFrameBudgetTests
{
    [Fact]
    public void ResolveWorkerCount_UsesWorkerBudgetMode()
    {
        var budget = new NprFrameBudget(
            WorkerBudgetMode: WorkerBudgetMode.SingleThreadDeterministic);

        var resolved = budget.ResolveWorkerCount();

        Assert.Equal(1, resolved);
    }

    [Fact]
    public void ResolveWorkerCount_ExplicitWorkersClamp()
    {
        var budget = new NprFrameBudget(
            MaxWorkerThreads: 20,
            MinimumWorkerThreads: 2,
            MaximumWorkerThreads: 8,
            WorkerBudgetMode: WorkerBudgetMode.Performance);

        var resolved = budget.ResolveWorkerCount();

        var expected = WorkerBudget.ResolveForLogicalProcessorCount(
            budget.ToWorkerBudgetRequest(),
            WorkerBudget.LogicalProcessorCount);

        Assert.Equal(expected, resolved);
    }

    [Fact]
    public void ResolveWorkerCount_MinMaxClamp()
    {
        var budget = new NprFrameBudget(
            MaxWorkerThreads: 0,
            MinimumWorkerThreads: 8,
            MaximumWorkerThreads: 4,
            WorkerBudgetMode: WorkerBudgetMode.Balanced);

        var resolved = budget.ResolveWorkerCount();

        var expected = WorkerBudget.ResolveForLogicalProcessorCount(
            budget.ToWorkerBudgetRequest(),
            WorkerBudget.LogicalProcessorCount);

        Assert.Equal(expected, resolved);
    }

    [Fact]
    public void ToWorkerBudgetRequest_MapsFields()
    {
        var budget = new NprFrameBudget(
            MaxWorkerThreads: 12,
            MinimumWorkerThreads: 3,
            MaximumWorkerThreads: 9,
            WorkerBudgetMode: WorkerBudgetMode.Benchmark);

        var request = budget.ToWorkerBudgetRequest();

        Assert.Equal(WorkerBudgetMode.Benchmark, request.Mode);
        Assert.Equal(12, request.ExplicitWorkerCount);
        Assert.Equal(3, request.MinimumWorkers);
        Assert.Equal(9, request.MaximumWorkers);
    }
}
