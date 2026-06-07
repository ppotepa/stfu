using System.Numerics;

namespace STFU.Common.Math;

public static class VectorMath
{
    public const float DefaultNormalizeEpsilonSquared = 0.0001f;
    public const float StrictNormalizeEpsilonSquared = 1e-12f;

    public static Vector2 NormalizeOrDefault(
        Vector2 value,
        Vector2 fallback,
        float epsilonSquared = DefaultNormalizeEpsilonSquared)
    {
        return value.LengthSquared() <= epsilonSquared ? fallback : Vector2.Normalize(value);
    }

    public static Vector3 NormalizeOrDefault(
        Vector3 value,
        Vector3 fallback,
        float epsilonSquared = DefaultNormalizeEpsilonSquared)
    {
        return value.LengthSquared() <= epsilonSquared ? fallback : Vector3.Normalize(value);
    }

    public static Vector4 NormalizeOrDefault(
        Vector4 value,
        Vector4 fallback,
        float epsilonSquared = DefaultNormalizeEpsilonSquared)
    {
        return value.LengthSquared() <= epsilonSquared ? fallback : Vector4.Normalize(value);
    }
}
