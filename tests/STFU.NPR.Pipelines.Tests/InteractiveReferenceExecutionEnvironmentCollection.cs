using Xunit;

namespace STFU.NPR.Pipelines.Tests;

[CollectionDefinition(Name)]
public sealed class InteractiveReferenceExecutionEnvironmentCollection : ICollectionFixture<InteractiveReferenceExecutionEnvironmentFixture>
{
    public const string Name = "InteractiveReferenceExecutionEnvironment";
}

public sealed class InteractiveReferenceExecutionEnvironmentFixture
{
}
