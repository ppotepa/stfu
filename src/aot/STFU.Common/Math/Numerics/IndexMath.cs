namespace STFU.Common.Math;

public static class IndexMath
{
    public static bool TryResolveOneBasedOrRelativeIndex(int parsedIndex, int count, out int zeroBasedIndex)
    {
        zeroBasedIndex = parsedIndex > 0 ? parsedIndex - 1 : count + parsedIndex;
        return zeroBasedIndex >= 0 && zeroBasedIndex < count;
    }
}
