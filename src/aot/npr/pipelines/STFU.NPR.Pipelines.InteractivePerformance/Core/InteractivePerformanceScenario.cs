namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed record InteractivePerformanceScenario
{
    public required string Name { get; init; }
    public string AssetPath { get; init; } = "assets/walking.fbx";
    public int Width { get; init; } = 320;
    public int Height { get; init; } = 240;
    public int Frames { get; init; } = 6;
    public double TargetFrameMs { get; init; } = 16.6;

    public static InteractivePerformanceScenario Create(string name, int width, int height, int frames, double targetFrameMs)
    {
        return new InteractivePerformanceScenario
        {
            Name = name,
            Width = width,
            Height = height,
            Frames = frames,
            TargetFrameMs = targetFrameMs
        };
    }
}
