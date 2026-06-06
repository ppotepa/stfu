using System.Numerics;

namespace STFU.Common.Math;

public static class AnimationSamplingMath
{
    public static double PositiveModulo(double value, double modulus)
    {
        if (modulus <= 0d)
        {
            return 0d;
        }

        var result = value % modulus;
        return result < 0d ? result + modulus : result;
    }

    public static int SampleCount(double durationSeconds, double sampleRateHz)
    {
        return NumericMath.AtLeast((int)global::System.Math.Ceiling(durationSeconds * sampleRateHz) + 1, 2);
    }

    public static int MaxSampleCount(int sampleCount, long memoryBudgetBytes, int vertexCount, int approximateVertexBytes)
    {
        var bytesPerSample = NumericMath.AtLeast(vertexCount * approximateVertexBytes, 1);
        var budgetSamples = NumericMath.AtLeast((int)(memoryBudgetBytes / bytesPerSample), 2);
        return NumericMath.AtLeast(global::System.Math.Min(sampleCount, budgetSamples), 2);
    }

    public static int ClampSampleIndex(int sampleIndex, int sampleCount)
    {
        return NumericMath.Clamp(sampleIndex, 0, NumericMath.AtLeast(sampleCount, 1) - 1);
    }

    public static double SampleTime(int sampleIndex, double sampleRateHz, double durationSeconds)
    {
        return NumericMath.AtMost(sampleIndex / sampleRateHz, durationSeconds);
    }

    public static int LowerSampleIndex(double samplePosition)
    {
        return (int)NumericMath.Floor(samplePosition);
    }

    public static int UpperSampleIndex(double samplePosition)
    {
        return (int)NumericMath.Ceiling(samplePosition);
    }

    public static int RoundedSampleIndex(double samplePosition)
    {
        return (int)NumericMath.Round(samplePosition);
    }

    public static float SampleInterpolationT(double samplePosition, int lowerIndex, int upperIndex)
    {
        return (float)((samplePosition - lowerIndex) / NumericMath.AtLeast(upperIndex - lowerIndex, 1));
    }

    public static int NearestSampleIndex(double samplePosition, int lowerIndex, int upperIndex)
    {
        return samplePosition - lowerIndex <= upperIndex - samplePosition ? lowerIndex : upperIndex;
    }

    public static Vector3 NormalizeOrDefault(Vector3 value, Vector3 fallback, float epsilonSquared = 1e-12f)
    {
        return value.LengthSquared() <= epsilonSquared ? fallback : Vector3.Normalize(value);
    }

    public static Vector3 InterpolateNormal(Vector3 lower, Vector3 upper, float t, Vector3 fallback)
    {
        return NormalizeOrDefault(Vector3.Lerp(lower, upper, t), fallback);
    }
}
