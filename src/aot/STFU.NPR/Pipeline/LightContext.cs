using System.Numerics;

namespace STFU.NPR.Pipeline;

public readonly record struct LightContext(
    Vector3 Direction,
    float Intensity)
{
    public static LightContext Default { get; } = new(
        Vector3.Normalize(new Vector3(-0.35f, 0.7f, -0.45f)),
        1f);
}
