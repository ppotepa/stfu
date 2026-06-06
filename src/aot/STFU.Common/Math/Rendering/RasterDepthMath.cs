namespace STFU.Common.Math;

public static class RasterDepthMath
{
    public const float DefaultDepthTieEpsilon = 1e-7f;

    public static bool ShouldWriteDepth(
        float newDepth,
        float currentDepth,
        int newFace,
        int currentFace,
        float tieEpsilon = DefaultDepthTieEpsilon)
    {
        return newDepth is >= 0f and <= 1f &&
               (currentFace < 0 ||
                newDepth < currentDepth ||
                (NumericMath.Abs(newDepth - currentDepth) <= tieEpsilon && newFace < currentFace));
    }
}
