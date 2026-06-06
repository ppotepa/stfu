namespace STFU.Common.Math;

public static class SafeReadMath
{
    public static T GetOrDefault<T>(IReadOnlyList<T> values, int index, T fallback = default!)
    {
        return (uint)index < (uint)values.Count
            ? values[index]
            : fallback;
    }

    public static float GetOrZero(IReadOnlyList<float> values, int index)
    {
        return GetOrDefault(values, index, 0f);
    }
}
