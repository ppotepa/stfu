using STFU.Common.Math;

namespace STFU.UI.Bridge.Scene;

internal static class SceneTransformMath
{
    public const float RotationDegreesMin = 0f;
    public const float RotationDegreesMax = 360f;
    public const float RotationIncrementDegrees = 0.1f;
    public const float ScaleMinimum = 0f;
    public const float ScaleIncrement = 0.1f;

    public static float ToRadians(float degrees)
    {
        return NumericMath.DegreesToRadians(NormalizeDegrees(degrees));
    }

    public static float ToDegrees(float radians)
    {
        var degrees = NumericMath.RadiansToDegrees(radians);
        return NormalizeDegrees(degrees);
    }

    public static float NormalizeDegrees(float degrees)
    {
        degrees %= RotationDegreesMax;
        if (degrees < 0f)
        {
            degrees += RotationDegreesMax;
        }

        return degrees;
    }
}
