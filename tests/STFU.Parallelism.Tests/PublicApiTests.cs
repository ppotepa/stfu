using STFU.Parallelism;
using Xunit;

namespace STFU.Parallelism.Tests;

public sealed class PublicApiTests
{
    [Fact]
    public void ExportsOnlyGeneralParallelismTypes()
    {
        var exportedTypes = typeof(WorkerBudget).Assembly
            .GetExportedTypes()
            .Where(type => type.Namespace == "STFU.Parallelism")
            .Select(type => type.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[]
            {
                nameof(DeterministicParallel),
                nameof(ParallelRange),
                nameof(PrefixSums),
                nameof(WorkerBudget),
                nameof(WorkerBudgetMode),
                nameof(WorkerBudgetRequest)
            },
            exportedTypes);
    }
}
