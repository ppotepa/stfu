using System.Numerics;
using STFU.Common.Math;

namespace STFU.NPR.Pipeline;

public readonly record struct LightContext(
    Vector3 Direction,
    float Intensity)
{
    public static LightContext Default { get; } = new(
        Geometry3D.NormalizeOrDefault(new Vector3(-0.35f, 0.7f, -0.45f), Vector3.UnitY),
        1f);
}
