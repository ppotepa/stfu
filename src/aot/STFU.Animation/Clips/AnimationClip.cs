namespace STFU.Animation.Clips;

public sealed record AnimationClip(
    string Name,
    double DurationSeconds,
    double TicksPerSecond,
    IReadOnlyList<NodeAnimationTrack> Tracks)
{
    public static AnimationClip Empty { get; } = new("empty", 0, 0, []);
}
