namespace STFU.Common.Math;

public static class PixelDiffMath
{
    public static int MaxChannelDelta(int bDelta, int gDelta, int rDelta, int aDelta)
    {
        return NumericMath.AtLeast(NumericMath.AtLeast(bDelta, gDelta), NumericMath.AtLeast(rDelta, aDelta));
    }

    public static string? FirstChannelName(int bDelta, int gDelta, int rDelta, int aDelta, byte tolerance)
    {
        if (bDelta > tolerance)
        {
            return "B";
        }

        if (gDelta > tolerance)
        {
            return "G";
        }

        if (rDelta > tolerance)
        {
            return "R";
        }

        if (aDelta > tolerance)
        {
            return "A";
        }

        return null;
    }
}
