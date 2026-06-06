using System.Numerics;

namespace STFU.Common.Math;

public readonly record struct QuantizedVector3Key(long X, long Y, long Z)
{
    public static QuantizedVector3Key From(Vector3 position, float scale = 100000f)
    {
        return new QuantizedVector3Key(
            (long)MathF.Round(position.X * scale),
            (long)MathF.Round(position.Y * scale),
            (long)MathF.Round(position.Z * scale));
    }
}

public static class MeshTopologyMath
{
    public static long CreateUndirectedEdgeKey(int a, int b)
    {
        var min = global::System.Math.Min(a, b);
        var max = global::System.Math.Max(a, b);
        return ((long)min << 32) | (uint)max;
    }

    public static int UndirectedEdgeStart(int a, int b)
    {
        return global::System.Math.Min(a, b);
    }

    public static int UndirectedEdgeEnd(int a, int b)
    {
        return global::System.Math.Max(a, b);
    }
}
