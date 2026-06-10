namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed record InteractiveBenchmarkScenario(
    string Name,
    string AssetPath,
    int Width,
    int Height,
    int Frames,
    int WarmupFrames)
{
    public static InteractiveBenchmarkScenario WalkingPreview { get; } = new(
        "walking-preview",
        "assets/walking.fbx",
        Width: 320,
        Height: 240,
        Frames: 12,
        WarmupFrames: 1);

    public static IReadOnlyList<InteractiveBenchmarkScenario> DefaultSuite { get; } = new[]
    {
        WalkingPreview,
        WalkingPreview with { Name = "walking-balanced", Width = 640, Height = 480, Frames = 12 },
        WalkingPreview with { Name = "walking-quality", Width = 960, Height = 540, Frames = 8 }
    };

    public string ResolutionLabel => $"{Width}x{Height}";
}
