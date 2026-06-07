using System.Numerics;

namespace STFU.Common.Math;

public static class FrameCacheHash
{
    public static ulong Start() => HashMath.FnvOffset64;

    public static ulong Add(ulong hash, int value) => HashMath.Fnv1A(hash, value);

    public static ulong Add(ulong hash, uint value) => HashMath.Fnv1A(hash, value);

    public static ulong Add(ulong hash, long value) => HashMath.Fnv1A(hash, value);

    public static ulong Add(ulong hash, ulong value) => HashMath.Fnv1A(hash, value);

    public static ulong Add(ulong hash, float value) => HashMath.Fnv1A(hash, value);

    public static ulong Add(ulong hash, double value) => HashMath.Fnv1A(hash, value);

    public static ulong Add(ulong hash, Vector2 value)
    {
        hash = Add(hash, value.X);
        hash = Add(hash, value.Y);
        return hash;
    }

    public static ulong Add(ulong hash, Vector3 value)
    {
        hash = Add(hash, value.X);
        hash = Add(hash, value.Y);
        hash = Add(hash, value.Z);
        return hash;
    }

    public static ulong Add(ulong hash, Quaternion value)
    {
        hash = Add(hash, value.X);
        hash = Add(hash, value.Y);
        hash = Add(hash, value.Z);
        hash = Add(hash, value.W);
        return hash;
    }
}
