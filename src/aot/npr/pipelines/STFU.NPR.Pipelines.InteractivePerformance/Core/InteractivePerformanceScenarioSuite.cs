namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public static class InteractivePerformanceScenarioSuite
{
    public static IReadOnlyList<InteractivePerformanceScenario> DefaultViewportScenarios { get; } =
    [
        InteractivePerformanceScenario.Create("walking-preview", 320, 240, 6, 16.6),
        InteractivePerformanceScenario.Create("walking-balanced", 640, 480, 6, 16.6),
        InteractivePerformanceScenario.Create("walking-quality", 960, 540, 4, 24.0),
        InteractivePerformanceScenario.Create("walking-stress", 1280, 720, 3, 33.3)
    ];

    public static InteractivePerformanceScenario? Find(string name)
    {
        return DefaultViewportScenarios.FirstOrDefault(scenario => string.Equals(scenario.Name, name, StringComparison.Ordinal));
    }
}
