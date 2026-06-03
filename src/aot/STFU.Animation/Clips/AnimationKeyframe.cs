using System.Numerics;

namespace STFU.Animation.Clips;

public sealed record AnimationKeyframe(
    double TimeSeconds,
    Vector3 Translation,
    Quaternion Rotation,
    Vector3 Scale)
{
    public static AnimationKeyframe Identity(double timeSeconds) => new(
        timeSeconds,
        Vector3.Zero,
        Quaternion.Identity,
        Vector3.One);
}
