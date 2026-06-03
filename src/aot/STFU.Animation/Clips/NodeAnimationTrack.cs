namespace STFU.Animation.Clips;

public sealed record NodeAnimationTrack(
    int TargetIndex,
    string TargetName,
    IReadOnlyList<AnimationKeyframe> Keyframes);
